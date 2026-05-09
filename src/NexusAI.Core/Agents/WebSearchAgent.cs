using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class WebSearchAgent : AgentBase
{
    public override string AgentType => AgentTypes.WebSearch;

    public WebSearchAgent(Kernel kernel) : base(kernel) { }

    public override async Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        progress.Report($"[WebSearch] Starting: {task.Title}");

        var system = """
            You are a web research specialist. Find and summarise relevant
            current information about the topic provided.
            Cite specific facts, statistics, and sources where possible.
            Structure findings clearly with source attribution.
            """;

        var user = $"""
            Research task: {task.Description}
            Original user request: {session.UserPrompt}

            Find and summarise:
            1. Key facts and current information
            2. Recent developments or trends
            3. Expert opinions or industry consensus
            4. Relevant statistics or data points
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[WebSearch] Complete");
        return result;
    }
}
