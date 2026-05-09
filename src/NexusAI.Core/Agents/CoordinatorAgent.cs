using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using System.Text.Json;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class CoordinatorAgent : AgentBase
{
    public override string AgentType => AgentTypes.Coordinator;

    public CoordinatorAgent(Kernel kernel) : base(kernel) { }

    public async Task<List<AgentTask>> DecomposeAsync(
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var system = """
            You are an expert task coordinator. Your job is to break down a complex
            user request into a sequence of specialist subtasks.

            Available agents:
            - Document: reads and extracts information from uploaded documents
            - WebSearch: searches the web for current information
            - Analysis: analyses data, finds patterns, and identifies insights
            - Report: synthesises all findings into a structured final report

            Respond ONLY with a valid JSON array. No markdown, no explanation.
            Format:
            [
              {"agentType": "Document", "title": "...", "description": "...", "order": 1},
              {"agentType": "Analysis", "title": "...", "description": "...", "order": 2},
              {"agentType": "Report",   "title": "...", "description": "...", "order": 3}
            ]
            """;

        var user = $"User request: {session.UserPrompt}";

        progress.Report($"[Coordinator] Decomposing task: {session.UserPrompt}");

        var json = await CompleteAsync(system, user, progress, ct);

        json = json.Trim();
        if (json.StartsWith("```")) json = System.Text.RegularExpressions.Regex
            .Replace(json, @"```[a-z]*\n?", "").Trim('`').Trim();

        var taskDefs = JsonSerializer.Deserialize<List<TaskDefinition>>(json)
            ?? new List<TaskDefinition>();

        var tasks = taskDefs.Select(t => new AgentTask
        {
            SessionId   = session.Id,
            AgentType   = t.AgentType,
            Title       = t.Title,
            Description = t.Description,
            Order       = t.Order,
            Status      = AgentTaskStatus.Pending
        }).OrderBy(t => t.Order).ToList();

        progress.Report($"[Coordinator] Created {tasks.Count} subtasks");
        return tasks;
    }

    public override Task<string> ExecuteAsync(
        AgentTask task, AgentSession session,
        IProgress<string> progress, CancellationToken ct)
        => Task.FromResult("Coordinator does not execute tasks directly.");

    private record TaskDefinition(
        string AgentType, string Title, string Description, int Order);
}
