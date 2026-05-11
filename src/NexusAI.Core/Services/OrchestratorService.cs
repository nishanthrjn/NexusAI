using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusAI.Core.Agents;
using NexusAI.Core.Agents.Interfaces;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using NexusAI.Domain.Interfaces;

namespace NexusAI.Core.Services;

public class OrchestratorService : IOrchestrator
{
    private static readonly ActivitySource _tracer = new("NexusAI");

    private readonly CoordinatorAgent        _coordinator;
    private readonly IAgentFactory           _factory;
    private readonly IAgentSessionRepository _sessionRepo;
    private readonly IAgentTaskRepository    _taskRepo;
    private readonly IMessageRepository      _messageRepo;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        CoordinatorAgent             coordinator,
        IAgentFactory                factory,
        IAgentSessionRepository      sessionRepo,
        IAgentTaskRepository         taskRepo,
        IMessageRepository           messageRepo,
        ILogger<OrchestratorService> logger)
    {
        _coordinator = coordinator;
        _factory     = factory;
        _sessionRepo = sessionRepo;
        _taskRepo    = taskRepo;
        _messageRepo = messageRepo;
        _logger      = logger;
    }

    public async Task<Guid> StartSessionAsync(string userPrompt, CancellationToken ct)
    {
        var session = new AgentSession { UserPrompt = userPrompt, Status = "Running" };
        await _sessionRepo.CreateAsync(session, ct);
        _logger.LogInformation("Session {Id} started: {Prompt}", session.Id, userPrompt);

        _ = Task.Run(() => RunSessionAsync(session, CancellationToken.None));

        return session.Id;
    }

    private async Task RunSessionAsync(AgentSession session, CancellationToken ct)
    {
        using var sessionSpan = _tracer.StartActivity("session")!;
        sessionSpan?.SetTag("session.id",     session.Id.ToString());
        sessionSpan?.SetTag("session.prompt", session.UserPrompt);

        try
        {
            var progress = new Progress<string>(msg =>
                _logger.LogInformation("[{Session}] {Message}", session.Id, msg));

            // Coordinator decomposes
            using (var coordSpan = _tracer.StartActivity("coordinator.decompose"))
            {
                coordSpan?.SetTag("session.id", session.Id.ToString());
                var tasks = await _coordinator.DecomposeAsync(session, progress, ct);
                foreach (var task in tasks)
                {
                    await _taskRepo.CreateAsync(task, ct);
                    session.Tasks.Add(task);
                }
                coordSpan?.SetTag("tasks.count", session.Tasks.Count.ToString());
            }

            // Run each agent
            foreach (var task in session.Tasks.OrderBy(t => t.Order))
            {
                task.Status = AgentTaskStatus.Running;
                await _taskRepo.UpdateAsync(task, ct);

                using var agentSpan = _tracer.StartActivity($"agent.{task.AgentType.ToLower()}");
                agentSpan?.SetTag("agent.type",  task.AgentType);
                agentSpan?.SetTag("agent.title", task.Title);
                agentSpan?.SetTag("session.id",  session.Id.ToString());

                var sw = Stopwatch.StartNew();
                try
                {
                    var agent  = _factory.GetAgent(task.AgentType);
                    var result = await agent.ExecuteAsync(task, session, progress, ct);

                    sw.Stop();
                    task.Status      = AgentTaskStatus.Completed;
                    task.Result      = result;
                    task.CompletedAt = DateTime.UtcNow;

                    agentSpan?.SetTag("agent.duration_ms", sw.ElapsedMilliseconds.ToString());
                    agentSpan?.SetTag("agent.status", "completed");

                    _logger.LogInformation("[{Session}] {Agent} completed in {Ms}ms",
                        session.Id, task.AgentType, sw.ElapsedMilliseconds);

                    await _messageRepo.AddAsync(new AgentMessage
                    {
                        SessionId = session.Id,
                        TaskId    = task.Id,
                        AgentType = task.AgentType,
                        Role      = "assistant",
                        Content   = result
                    }, ct);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    task.Status = AgentTaskStatus.Failed;
                    task.Error  = ex.Message;
                    agentSpan?.SetTag("agent.status", "failed");
                    agentSpan?.SetTag("agent.error",  ex.Message);
                    _logger.LogError(ex, "Task {TaskId} failed after {Ms}ms",
                        task.Id, sw.ElapsedMilliseconds);
                }

                await _taskRepo.UpdateAsync(task, ct);
            }

            session.Status      = "Completed";
            session.CompletedAt = DateTime.UtcNow;
            session.FinalReport = session.Tasks
                .FirstOrDefault(t => t.AgentType == AgentType.Report)?.Result;

            sessionSpan?.SetTag("session.status", "completed");
            await _sessionRepo.UpdateAsync(session, ct);
            _logger.LogInformation("Session {Id} completed", session.Id);
        }
        catch (Exception ex)
        {
            session.Status = "Failed";
            sessionSpan?.SetTag("session.status", "failed");
            await _sessionRepo.UpdateAsync(session, ct);
            _logger.LogError(ex, "Session {Id} failed", session.Id);
        }
    }
}
