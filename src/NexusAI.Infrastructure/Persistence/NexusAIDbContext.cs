using Microsoft.EntityFrameworkCore;
using NexusAI.Domain.Entities;

namespace NexusAI.Infrastructure.Persistence;

public class NexusAIDbContext : DbContext
{
    public NexusAIDbContext(DbContextOptions<NexusAIDbContext> options)
        : base(options) { }

    public DbSet<AgentSession> AgentSessions { get; set; }
    public DbSet<AgentTask>    AgentTasks    { get; set; }
    public DbSet<AgentMessage> AgentMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentSession>(b =>
        {
            b.ToTable("agent_sessions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserPrompt).HasColumnName("user_prompt").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            b.Property(x => x.FinalReport).HasColumnName("final_report");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.HasMany(x => x.Tasks)
             .WithOne(x => x.Session)
             .HasForeignKey(x => x.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Messages)
             .WithOne(x => x.Session)
             .HasForeignKey(x => x.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentTask>(b =>
        {
            b.ToTable("agent_tasks");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.SessionId).HasColumnName("session_id");
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            b.Property(x => x.AgentType).HasColumnName("agent_type").HasMaxLength(100);
            b.Property(x => x.Result).HasColumnName("result");
            b.Property(x => x.Error).HasColumnName("error");
            b.Property(x => x.Order).HasColumnName("order");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.HasMany(x => x.Messages)
             .WithOne()
             .HasForeignKey(x => x.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentMessage>(b =>
        {
            b.ToTable("agent_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.SessionId).HasColumnName("session_id");
            b.Property(x => x.TaskId).HasColumnName("task_id");
            b.Property(x => x.AgentType).HasColumnName("agent_type").HasMaxLength(100);
            b.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
            b.Property(x => x.Content).HasColumnName("content");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.HasIndex(x => x.SessionId)
             .HasDatabaseName("ix_agent_messages_session_id");
        });

        base.OnModelCreating(modelBuilder);
    }
}
