using NexusAI.Domain.Entities;

namespace NexusAI.Core.Agents.Interfaces;

public interface IAgent
{
    string AgentType { get; }

    Task<string> ExecuteAsync(
        AgentTask           task,
        AgentSession        session,
        IProgress<string>   progress,
        CancellationToken   ct);
}
