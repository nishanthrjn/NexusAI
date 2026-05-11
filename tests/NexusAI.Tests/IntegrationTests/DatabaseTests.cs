using Microsoft.EntityFrameworkCore;
using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using NexusAI.Infrastructure.Persistence;
using NexusAI.Infrastructure.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace NexusAI.Tests.IntegrationTests;

public class DatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("nexusai_test")
        .WithUsername("nexusai")
        .WithPassword("nexusai_test")
        .Build();

    private NexusAIDbContext _db = null!;
    private IDbContextFactory<NexusAIDbContext> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<NexusAIDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _db = new NexusAIDbContext(options);
        await _db.Database.MigrateAsync();

        _factory = new TestDbContextFactory(options);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CanCreateAndRetrieveAgentSession()
    {
        var repo    = new AgentSessionRepository(_factory);
        var session = new AgentSession { UserPrompt = "Test prompt", Status = "Running" };

        var created = await repo.CreateAsync(session, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, created.Id);

        var retrieved = await repo.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal("Test prompt", retrieved.UserPrompt);
    }

    [Fact]
    public async Task CanUpdateSessionStatus()
    {
        var repo    = new AgentSessionRepository(_factory);
        var session = new AgentSession { UserPrompt = "Update test", Status = "Running" };

        await repo.CreateAsync(session, CancellationToken.None);
        session.Status      = "Completed";
        session.FinalReport = "Final report content.";
        session.CompletedAt = DateTime.UtcNow;
        await repo.UpdateAsync(session, CancellationToken.None);

        var updated = await repo.GetByIdAsync(session.Id, CancellationToken.None);
        Assert.Equal("Completed", updated!.Status);
        Assert.Equal("Final report content.", updated.FinalReport);
    }

    [Fact]
    public async Task CanCreateAndRetrieveAgentTask()
    {
        var sessionRepo = new AgentSessionRepository(_factory);
        var taskRepo    = new AgentTaskRepository(_factory);

        var session = await sessionRepo.CreateAsync(
            new AgentSession { UserPrompt = "Task test", Status = "Running" },
            CancellationToken.None);

        var task = new AgentTask
        {
            SessionId   = session.Id,
            AgentType   = AgentType.WebSearch,
            Title       = "Search for information",
            Description = "Research the topic",
            Order       = 1,
            Status      = AgentTaskStatus.Pending
        };

        await taskRepo.CreateAsync(task, CancellationToken.None);
        var tasks = await taskRepo.GetBySessionAsync(session.Id, CancellationToken.None);

        Assert.Single(tasks);
        Assert.Equal("Search for information", tasks[0].Title);
    }

    [Fact]
    public async Task CanCompleteAgentTask()
    {
        var sessionRepo = new AgentSessionRepository(_factory);
        var taskRepo    = new AgentTaskRepository(_factory);

        var session = await sessionRepo.CreateAsync(
            new AgentSession { UserPrompt = "Complete task test", Status = "Running" },
            CancellationToken.None);

        var task = await taskRepo.CreateAsync(new AgentTask
        {
            SessionId   = session.Id,
            AgentType   = AgentType.Analysis,
            Title       = "Analyse data",
            Description = "Find patterns",
            Order       = 1,
            Status      = AgentTaskStatus.Pending
        }, CancellationToken.None);

        task.Status      = AgentTaskStatus.Completed;
        task.Result      = "Analysis complete.";
        task.CompletedAt = DateTime.UtcNow;
        await taskRepo.UpdateAsync(task, CancellationToken.None);

        var tasks = await taskRepo.GetBySessionAsync(session.Id, CancellationToken.None);
        Assert.Equal(AgentTaskStatus.Completed, tasks[0].Status);
        Assert.NotNull(tasks[0].Result);
    }

    [Fact]
    public async Task CanAddAndRetrieveAgentMessages()
    {
        var sessionRepo = new AgentSessionRepository(_factory);
        var messageRepo = new MessageRepository(_factory);

        var session = await sessionRepo.CreateAsync(
            new AgentSession { UserPrompt = "Message test", Status = "Running" },
            CancellationToken.None);

        await messageRepo.AddAsync(new AgentMessage
        {
            SessionId = session.Id,
            AgentType = AgentType.WebSearch,
            Role      = "assistant",
            Content   = "I found the following information..."
        }, CancellationToken.None);

        var messages = await messageRepo.GetBySessionAsync(session.Id, CancellationToken.None);
        Assert.Single(messages);
        Assert.Equal("I found the following information...", messages[0].Content);
    }

    [Fact]
    public async Task GetRecentSessionsReturnsLatestFirst()
    {
        var repo = new AgentSessionRepository(_factory);

        for (int i = 1; i <= 5; i++)
        {
            await repo.CreateAsync(
                new AgentSession { UserPrompt = $"Session {i}", Status = "Completed" },
                CancellationToken.None);
            await Task.Delay(10);
        }

        var recent = await repo.GetRecentAsync(3, CancellationToken.None);
        Assert.Equal(3, recent.Count);
        Assert.Equal("Session 5", recent[0].UserPrompt);
    }
}

public class TestDbContextFactory : IDbContextFactory<NexusAIDbContext>
{
    private readonly DbContextOptions<NexusAIDbContext> _options;
    public TestDbContextFactory(DbContextOptions<NexusAIDbContext> options) => _options = options;
    public NexusAIDbContext CreateDbContext() => new(_options);
}
