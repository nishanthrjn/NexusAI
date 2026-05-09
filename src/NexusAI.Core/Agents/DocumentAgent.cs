using Microsoft.SemanticKernel;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using AgentTypes = NexusAI.Domain.Enums.AgentType;

namespace NexusAI.Core.Agents;

public class DocumentAgent : AgentBase
{
    public override string AgentType => AgentTypes.Document;

    public DocumentAgent(Kernel kernel) : base(kernel) { }

    public override async Task<string> ExecuteAsync(
        AgentTask         task,
        AgentSession      session,
        IProgress<string> progress,
        CancellationToken ct)
    {
        progress.Report($"[Document] Starting: {task.Title}");

        var system = """
            You are a document analysis specialist. Extract and structure
            key information from documents. Identify main topics, entities,
            facts, figures, and relationships. Be precise and thorough.
            """;

        var user = $"""
            Task: {task.Description}
            Original request: {session.UserPrompt}

            Extract and structure:
            1. Main topics and themes
            2. Key facts and figures
            3. Important entities (people, organisations, dates)
            4. Relationships and dependencies
            5. Conclusions or recommendations in the document
            """;

        var result = await StreamCompleteAsync(system, user, progress, ct);
        progress.Report($"[Document] Complete");
        return result;
    }
}
