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
            .Select(t => $"[{t.AgentType}] {t.Title}:\n{t.Result}")
            .ToList();

        var context = previousResults.Any()
            ? string.Join("\n\n", previousResults)
            : "No previous results available.";

        var system = """
            You are an expert data analyst. Analyse the provided information
            and extract key insights, patterns, risks, and opportunities.
            Be specific and cite evidence from the provided context.
            Structure your response with clear sections and bullet points.
            """;

        var user = $"""
            Task: {task.Description}

            Context from previous agents:
            {context}

            Provide a detailed analysis with:
            1. Key findings
            2. Identified risks or opportunities
            3. Supporting evidence
            4. Confidence level for each finding
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[Analysis] Complete");
        return result;
    }
}
