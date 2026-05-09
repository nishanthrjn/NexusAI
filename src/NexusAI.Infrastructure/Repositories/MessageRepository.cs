using Microsoft.EntityFrameworkCore;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Interfaces;
using NexusAI.Infrastructure.Persistence;

namespace NexusAI.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly IDbContextFactory<NexusAIDbContext> _factory;

    public MessageRepository(IDbContextFactory<NexusAIDbContext> factory)
        => _factory = factory;

    public async Task<AgentMessage> AddAsync(AgentMessage message, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AgentMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<List<AgentMessage>> GetBySessionAsync(Guid sessionId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AgentMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}
