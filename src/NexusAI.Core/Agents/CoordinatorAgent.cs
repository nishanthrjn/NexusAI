using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

            IMPORTANT: Respond ONLY with a valid complete JSON array.
            Keep it to 4 tasks maximum. No markdown, no explanation.
            Use exactly this format:
            [
              {"agentType":"WebSearch","title":"...","description":"...","order":1},
              {"agentType":"Analysis","title":"...","description":"...","order":2},
              {"agentType":"Report","title":"...","description":"...","order":3}
            ]
            """;

        var user = $"User request: {session.UserPrompt}";

        progress.Report($"[Coordinator] Decomposing task: {session.UserPrompt}");

        var raw = await CompleteAsync(system, user, progress, ct);

        // Clean markdown fences
        var json = raw.Trim();
        if (json.StartsWith("```"))
            json = Regex.Replace(json, @"```[a-z]*\n?", "").Trim('`').Trim();

        // Extract JSON array
        var startIdx = json.IndexOf('[');
        var endIdx   = json.LastIndexOf(']');

        if (startIdx < 0)
        {
            progress.Report("[Coordinator] No JSON array found — using default tasks");
            return GetDefaultTasks(session.Id, session.UserPrompt);
        }

        // If closing bracket is missing — truncated response — add it
        if (endIdx < startIdx)
        {
            json = json[startIdx..].TrimEnd().TrimEnd(',') + "]";
        }
        else
        {
            json = json[startIdx..(endIdx + 1)];
        }

        List<TaskDefinition>? taskDefs = null;
        try
        {
            taskDefs = JsonSerializer.Deserialize<List<TaskDefinition>>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            progress.Report($"[Coordinator] JSON parse error: {ex.Message} — using default tasks");
            return GetDefaultTasks(session.Id, session.UserPrompt);
        }

        var tasks = (taskDefs ?? new List<TaskDefinition>())
            .Where(t => !string.IsNullOrWhiteSpace(t.AgentType))
            .Select(t => new AgentTask
            {
                SessionId   = session.Id,
                AgentType   = t.AgentType   ?? "Analysis",
                Title       = t.Title       ?? "Untitled Task",
                Description = t.Description ?? t.Title ?? "No description",
                Order       = t.Order,
                Status      = AgentTaskStatus.Pending
            })
            .OrderBy(t => t.Order)
            .ToList();

        if (!tasks.Any())
            return GetDefaultTasks(session.Id, session.UserPrompt);

        // Always ensure a Report task at the end
        if (!tasks.Any(t => t.AgentType == AgentTypes.Report))
        {
            tasks.Add(new AgentTask
            {
                SessionId   = session.Id,
                AgentType   = AgentTypes.Report,
                Title       = "Final Report",
                Description = "Synthesise all findings into a comprehensive report",
                Order       = tasks.Max(t => t.Order) + 1,
                Status      = AgentTaskStatus.Pending
            });
        }

        progress.Report($"[Coordinator] Created {tasks.Count} subtasks");
        return tasks;
    }

    private List<AgentTask> GetDefaultTasks(Guid sessionId, string userPrompt = "") =>
        new()
        {
            new() { SessionId=sessionId, AgentType=AgentTypes.WebSearch,
                Title="Research", Description=$"Research information about: {userPrompt}",
                Order=1, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Analysis,
                Title="Analysis", Description=$"Analyse findings related to: {userPrompt}",
                Order=2, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Report,
                Title="Report", Description=$"Produce a comprehensive report answering: {userPrompt}",
                Order=3, Status=AgentTaskStatus.Pending }
        };

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
