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
            .Select(t => $"## {t.AgentType} Agent — {t.Title}\n\n{t.Result}")
            .ToList();

        var context = string.Join("\n\n---\n\n", allResults);

        var system = """
            You are an expert report writer. Synthesise all provided research
            and analysis into a clear, professional, structured report.
            Use markdown formatting with proper headings, bullet points, and sections.
            Every claim must be supported by the provided evidence.
            End with a clear, actionable executive summary.
            """;

        var user = $"""
            Original request: {session.UserPrompt}

            Findings from all agents:
            {context}

            Write a comprehensive report with:
            # Executive Summary
            # Key Findings
            # Detailed Analysis
            # Risks and Opportunities
            # Recommendations
            # Sources and Evidence
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[Report] Final report ready");
        return result;
    }
}
