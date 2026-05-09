using NexusAI.Domain.Entities;

namespace NexusAI.Domain.Interfaces;

public interface IMessageRepository
{
    Task<AgentMessage>       AddAsync(AgentMessage message, CancellationToken ct);
    Task<List<AgentMessage>> GetBySessionAsync(Guid sessionId, CancellationToken ct);
}
