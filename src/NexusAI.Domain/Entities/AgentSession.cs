namespace NexusAI.Domain.Entities;

public class AgentSession
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public string   UserPrompt  { get; set; } = string.Empty;
    public string   Status      { get; set; } = "Running";
    public string?  FinalReport { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<AgentTask>    Tasks    { get; set; } = new();
    public List<AgentMessage> Messages { get; set; } = new();
}
