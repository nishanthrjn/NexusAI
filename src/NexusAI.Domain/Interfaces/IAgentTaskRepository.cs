using NexusAI.Domain.Entities;

namespace NexusAI.Domain.Interfaces;

public interface IAgentTaskRepository
{
    Task<AgentTask>       CreateAsync(AgentTask task, CancellationToken ct);
    Task<List<AgentTask>> GetBySessionAsync(Guid sessionId, CancellationToken ct);
    Task                  UpdateAsync(AgentTask task, CancellationToken ct);
}
