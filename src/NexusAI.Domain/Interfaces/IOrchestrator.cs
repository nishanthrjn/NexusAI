namespace NexusAI.Domain.Interfaces;

public interface IOrchestrator
{
    Task<Guid> StartSessionAsync(string userPrompt, CancellationToken ct);
}
