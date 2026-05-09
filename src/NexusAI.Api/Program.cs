using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using NexusAI.Api.Hubs;
using NexusAI.Core.Agents;
using NexusAI.Core.Agents.Interfaces;
using NexusAI.Core.Services;
using NexusAI.Domain.Interfaces;
using NexusAI.Infrastructure.Persistence;
using NexusAI.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
var connectionString   = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=nexusai;Username=nexusai;Password=nexusai_dev";
var ollamaEndpoint     = builder.Configuration["Ollama:Endpoint"]  ?? "http://localhost:11434";
var ollamaChatModel    = builder.Configuration["Ollama:ChatModel"] ?? "llama3.2";

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContextFactory<NexusAIDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Semantic Kernel ───────────────────────────────────────────────────────────
#pragma warning disable SKEXP0070
var kernel = Kernel.CreateBuilder()
    .AddOllamaChatCompletion(ollamaChatModel, new Uri(ollamaEndpoint))
    .Build();
#pragma warning restore SKEXP0070

builder.Services.AddSingleton(kernel);

// ── Agents ────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<CoordinatorAgent>();
builder.Services.AddSingleton<IAgent, DocumentAgent>();
builder.Services.AddSingleton<IAgent, WebSearchAgent>();
builder.Services.AddSingleton<IAgent, AnalysisAgent>();
builder.Services.AddSingleton<IAgent, ReportAgent>();
builder.Services.AddSingleton<IAgentFactory, AgentFactory>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
builder.Services.AddScoped<IAgentTaskRepository,    AgentTaskRepository>();
builder.Services.AddScoped<IMessageRepository,      MessageRepository>();

// ── Orchestrator ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrchestrator, OrchestratorService>();

// ── API + SignalR ─────────────────────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<NexusAIDbContext>>();
    await using var db = factory.CreateDbContext();
    await db.Database.MigrateAsync();
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "NexusAI API";
    options.Theme = ScalarTheme.DeepSpace;
});

// ── SignalR Hub ───────────────────────────────────────────────────────────────
app.MapHub<AgentHub>("/hubs/agent");

// ── Endpoints ─────────────────────────────────────────────────────────────────

app.MapGet("/health", () => new
{
    Status  = "NexusAI Online",
    Agents  = new[] { "Coordinator", "Document", "WebSearch", "Analysis", "Report" },
    Time    = DateTime.UtcNow,
    Version = "1.0.0"
}).WithName("GetHealth").WithTags("System");

// POST /api/sessions — start a new agent session
app.MapPost("/api/sessions", async (
    StartSessionRequest request,
    IOrchestrator       orchestrator,
    CancellationToken   ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
        return Results.BadRequest(new { Error = "Prompt cannot be empty" });

    var sessionId = await orchestrator.StartSessionAsync(request.Prompt, ct);

    return Results.Accepted($"/api/sessions/{sessionId}", new
    {
        SessionId = sessionId,
        Status    = "Running",
        Message   = "Agents are working. Poll /api/sessions/{id} for progress.",
        SignalR   = $"/hubs/agent — join group '{sessionId}' for live updates"
    });
}).WithName("StartSession").WithTags("Sessions");

// GET /api/sessions — list recent sessions
app.MapGet("/api/sessions", async (
    IAgentSessionRepository repo,
    CancellationToken       ct) =>
{
    var sessions = await repo.GetRecentAsync(20, ct);
    return Results.Ok(sessions.Select(s => new
    {
        s.Id, s.Status, s.UserPrompt,
        s.CreatedAt, s.CompletedAt,
        TaskCount = s.Tasks.Count
    }));
}).WithName("GetSessions").WithTags("Sessions");

// GET /api/sessions/{id} — get session with all tasks and results
app.MapGet("/api/sessions/{id:guid}", async (
    Guid                    id,
    IAgentSessionRepository repo,
    CancellationToken       ct) =>
{
    var session = await repo.GetByIdAsync(id, ct);
    if (session is null)
        return Results.NotFound(new { Error = $"Session {id} not found" });

    return Results.Ok(new
    {
        session.Id,
        session.Status,
        session.UserPrompt,
        session.CreatedAt,
        session.CompletedAt,
        session.FinalReport,
        Tasks = session.Tasks.Select(t => new
        {
            t.Id, t.AgentType, t.Title,
            t.Status, t.Order,
            t.CreatedAt, t.CompletedAt,
            HasResult = t.Result != null,
            t.Error
        })
    });
}).WithName("GetSession").WithTags("Sessions");

// GET /api/sessions/{id}/tasks/{taskId} — get full task result
app.MapGet("/api/sessions/{id:guid}/tasks/{taskId:guid}", async (
    Guid                    id,
    Guid                    taskId,
    IAgentSessionRepository repo,
    CancellationToken       ct) =>
{
    var session = await repo.GetByIdAsync(id, ct);
    if (session is null)
        return Results.NotFound(new { Error = $"Session {id} not found" });

    var task = session.Tasks.FirstOrDefault(t => t.Id == taskId);
    if (task is null)
        return Results.NotFound(new { Error = $"Task {taskId} not found" });

    return Results.Ok(new
    {
        task.Id, task.AgentType, task.Title,
        task.Description, task.Status,
        task.Result, task.Error,
        task.CreatedAt, task.CompletedAt
    });
}).WithName("GetTaskResult").WithTags("Sessions");

app.Run();

public record StartSessionRequest(string Prompt);
