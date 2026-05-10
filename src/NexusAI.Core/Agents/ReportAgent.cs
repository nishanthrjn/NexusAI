using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class ReportAgent : AgentBase
{
    public override string AgentType => AgentTypes.Report;

    public ReportAgent(Kernel kernel) : base(kernel) { }

    public override async Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        progress.Report($"[Report] Synthesising final report");

        var allResults = session.Tasks
            .Where(t => t.AgentType != AgentTypes.Report && t.Result != null)
            .Select(t => $"## {t.AgentType}\n{Truncate(t.Result!, 800)}")
            .ToList();

        var context = string.Join("\n\n", allResults);

        var system = """
            You are a report writer. Write a clear, structured markdown report.
            Be concise — maximum 400 words total.
            """;

        var user = $"""
            Original request: {session.UserPrompt}
            Findings: {context}
            Write a report with: Executive Summary, Key Findings, Recommendations.
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[Report] Complete");
        return result;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
