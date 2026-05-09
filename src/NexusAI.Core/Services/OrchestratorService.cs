using Microsoft.Extensions.Logging;
using NexusAI.Core.Agents;
using NexusAI.Core.Agents.Interfaces;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using NexusAI.Domain.Interfaces;

namespace NexusAI.Core.Services;

public class OrchestratorService : IOrchestrator
{
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

    public async Task<Guid> StartSessionAsync(
        string userPrompt, CancellationToken ct)
    {
        // 1. Create session
        var session = new AgentSession
        {
            UserPrompt = userPrompt,
            Status     = "Running"
        };
        await _sessionRepo.CreateAsync(session, ct);
        _logger.LogInformation("Session {Id} started: {Prompt}", session.Id, userPrompt);

        // 2. Run in background — return session ID immediately
        _ = Task.Run(() => RunSessionAsync(session, ct), ct);

        return session.Id;
    }

    private async Task RunSessionAsync(AgentSession session, CancellationToken ct)
    {
        try
        {
            // 3. Coordinator decomposes the task
            var progress = new Progress<string>(msg =>
                _logger.LogInformation("[{Session}] {Message}", session.Id, msg));

            var tasks = await _coordinator.DecomposeAsync(session, progress, ct);

            // 4. Save tasks to database
            foreach (var task in tasks)
            {
                await _taskRepo.CreateAsync(task, ct);
                session.Tasks.Add(task);
            }

            // 5. Execute each task in order
            foreach (var task in tasks.OrderBy(t => t.Order))
            {
                task.Status = AgentTaskStatus.Running;
                await _taskRepo.UpdateAsync(task, ct);

                try
                {
                    var agent  = _factory.GetAgent(task.AgentType);
                    var result = await agent.ExecuteAsync(task, session, progress, ct);

                    task.Status      = AgentTaskStatus.Completed;
                    task.Result      = result;
                    task.CompletedAt = DateTime.UtcNow;

                    // Save message to conversation history
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
                    task.Status = AgentTaskStatus.Failed;
                    task.Error  = ex.Message;
                    _logger.LogError(ex, "Task {TaskId} failed", task.Id);
                }

                await _taskRepo.UpdateAsync(task, ct);
            }

            // 6. Mark session complete
            session.Status      = "Completed";
            session.CompletedAt = DateTime.UtcNow;
            session.FinalReport = session.Tasks
                .FirstOrDefault(t => t.AgentType == AgentType.Report)?.Result;

            await _sessionRepo.UpdateAsync(session, ct);
            _logger.LogInformation("Session {Id} completed", session.Id);
        }
        catch (Exception ex)
        {
            session.Status = "Failed";
            await _sessionRepo.UpdateAsync(session, ct);
            _logger.LogError(ex, "Session {Id} failed", session.Id);
        }
    }
}
