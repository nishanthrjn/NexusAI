using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class CoordinatorAgent : AgentBase
{
    public override string AgentType => AgentTypes.Coordinator;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoordinatorAgent(Kernel kernel) : base(kernel) { }

    public async Task<List<AgentTask>> DecomposeAsync(
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var system = """
            You are an expert task coordinator. Break down the user request
            into a sequence of specialist subtasks.

            Available agents:
            - Document: reads and extracts information from uploaded documents
            - WebSearch: searches the web for current information
            - Analysis: analyses data, finds patterns, identifies insights
            - Report: synthesises all findings into a structured final report

            Respond ONLY with a valid JSON array. No markdown, no explanation.
            Use exactly this format:
            [
              {"agentType": "WebSearch", "title": "...", "description": "...", "order": 1},
              {"agentType": "Analysis",  "title": "...", "description": "...", "order": 2},
              {"agentType": "Report",    "title": "...", "description": "...", "order": 3}
            ]
            """;

        var user = $"User request: {session.UserPrompt}";

        progress.Report($"[Coordinator] Decomposing task: {session.UserPrompt}");

        var json = await CompleteAsync(system, user, progress, ct);

        // Strip markdown fences if present
        json = json.Trim();
        if (json.StartsWith("```"))
            json = System.Text.RegularExpressions.Regex
                .Replace(json, @"```[a-z]*\n?", "").Trim('`').Trim();

        // Find JSON array — skip any preamble text
        var startIdx = json.IndexOf('[');
        var endIdx   = json.LastIndexOf(']');
        if (startIdx >= 0 && endIdx > startIdx)
            json = json[startIdx..(endIdx + 1)];

        var taskDefs = JsonSerializer.Deserialize<List<TaskDefinition>>(json, _jsonOptions)
            ?? new List<TaskDefinition>();

        var tasks = taskDefs
            .Where(t => !string.IsNullOrWhiteSpace(t.AgentType))
            .Select(t => new AgentTask
            {
                SessionId   = session.Id,
                AgentType   = t.AgentType ?? "Analysis",
                Title       = t.Title       ?? "Untitled Task",
                Description = t.Description ?? t.Title ?? "No description",
                Order       = t.Order,
                Status      = AgentTaskStatus.Pending
            })
            .OrderBy(t => t.Order)
            .ToList();

        progress.Report($"[Coordinator] Created {tasks.Count} subtasks");
        return tasks;
    }

    public override Task<string> ExecuteAsync(
        AgentTask task, AgentSession session,
        IProgress<string> progress, CancellationToken ct)
        => Task.FromResult("Coordinator does not execute tasks directly.");

    private record TaskDefinition(
        [property: JsonPropertyName("agentType")]   string?  AgentType,
        [property: JsonPropertyName("title")]        string?  Title,
        [property: JsonPropertyName("description")]  string?  Description,
        [property: JsonPropertyName("order")]        int      Order);
}
