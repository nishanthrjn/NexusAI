using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class AnalysisAgent : AgentBase
{
    public override string AgentType => AgentTypes.Analysis;

    public AnalysisAgent(Kernel kernel) : base(kernel) { }

    public override async Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        progress.Report($"[Analysis] Starting: {task.Title}");

        var previousResults = session.Tasks
            .Where(t => t.Order < task.Order && t.Result != null)
            .Select(t => $"[{t.AgentType}]:\n{Truncate(t.Result!, 1000)}")
            .ToList();

        var context = previousResults.Any()
            ? string.Join("\n\n", previousResults)
            : "No previous results.";

        var system = """
            You are a data analyst. Analyse the provided information briefly.
            Be concise — maximum 300 words. Focus only on key insights.
            """;

        var user = $"""
            Task: {task.Description}
            Context: {context}
            Provide: 3 key findings, 2 risks or opportunities, confidence level.
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[Analysis] Complete");
        return result;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
