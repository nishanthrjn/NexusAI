using Microsoft.EntityFrameworkCore;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Interfaces;
using NexusAI.Infrastructure.Persistence;

namespace NexusAI.Infrastructure.Repositories;

public class AgentTaskRepository : IAgentTaskRepository
{
    private readonly IDbContextFactory<NexusAIDbContext> _factory;

    public AgentTaskRepository(IDbContextFactory<NexusAIDbContext> factory)
        => _factory = factory;

    public async Task<AgentTask> CreateAsync(AgentTask task, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AgentTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<List<AgentTask>> GetBySessionAsync(Guid sessionId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AgentTasks
            .Where(t => t.SessionId == sessionId)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(AgentTask task, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AgentTasks.Update(task);
        await db.SaveChangesAsync(ct);
    }
}
