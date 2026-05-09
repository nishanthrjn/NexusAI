using NexusAI.Domain.Entities;

namespace NexusAI.Domain.Interfaces;

public interface IAgentSessionRepository
{
    Task<AgentSession>       CreateAsync(AgentSession session, CancellationToken ct);
    Task<AgentSession?>      GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<AgentSession>> GetRecentAsync(int count, CancellationToken ct);
    Task                     UpdateAsync(AgentSession session, CancellationToken ct);
}
