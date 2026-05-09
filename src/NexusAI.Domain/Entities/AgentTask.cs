namespace NexusAI.Domain.Entities;

public class AgentTask
{
    public Guid        Id          { get; set; } = Guid.NewGuid();
    public Guid        SessionId   { get; set; }
    public string      Title       { get; set; } = string.Empty;
    public string      Description { get; set; } = string.Empty;
    public string      Status      { get; set; } = "Pending";
    public string      AgentType   { get; set; } = string.Empty;
    public string?     Result      { get; set; }
    public string?     Error       { get; set; }
    public DateTime    CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime?   CompletedAt { get; set; }
    public int         Order       { get; set; }

    public AgentSession Session    { get; set; } = null!;
    public List<AgentMessage> Messages { get; set; } = new();
}
