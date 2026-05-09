using Microsoft.EntityFrameworkCore;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Interfaces;
using NexusAI.Infrastructure.Persistence;

namespace NexusAI.Infrastructure.Repositories;

public class AgentSessionRepository : IAgentSessionRepository
{
    private readonly IDbContextFactory<NexusAIDbContext> _factory;

    public AgentSessionRepository(IDbContextFactory<NexusAIDbContext> factory)
        => _factory = factory;

    public async Task<AgentSession> CreateAsync(AgentSession session, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AgentSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<AgentSession?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AgentSessions
            .Include(s => s.Tasks.OrderBy(t => t.Order))
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<AgentSession>> GetRecentAsync(int count, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AgentSessions
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(AgentSession session, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AgentSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
