namespace NexusAI.Domain.Entities;

public class AgentMessage
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public Guid     SessionId { get; set; }
    public Guid?    TaskId    { get; set; }
    public string   AgentType { get; set; } = string.Empty;
    public string   Role      { get; set; } = string.Empty;
    public string   Content   { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AgentSession Session { get; set; } = null!;
}
