using NexusAI.Domain.Entities;
using NexusAI.Domain.Enums;
using Xunit;

namespace NexusAI.Tests;

public class AgentTaskTests
{
    [Fact]
    public void AgentTask_DefaultStatus_IsPending()
    {
        var task = new AgentTask();
        Assert.Equal(AgentTaskStatus.Pending, task.Status);
    }

    [Fact]
    public void AgentTask_DefaultId_IsNotEmpty()
    {
        var task = new AgentTask();
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void AgentSession_DefaultStatus_IsRunning()
    {
        var session = new AgentSession { Status = "Running" };
        Assert.Equal("Running", session.Status);
    }

    [Fact]
    public void AgentSession_Tasks_DefaultsToEmptyList()
    {
        var session = new AgentSession();
        Assert.NotNull(session.Tasks);
        Assert.Empty(session.Tasks);
    }

    [Fact]
    public void AgentMessage_DefaultId_IsNotEmpty()
    {
        var msg = new AgentMessage();
        Assert.NotEqual(Guid.Empty, msg.Id);
    }

    [Fact]
    public void AgentTaskStatus_Constants_AreCorrect()
    {
        Assert.Equal("Pending",   AgentTaskStatus.Pending);
        Assert.Equal("Running",   AgentTaskStatus.Running);
        Assert.Equal("Completed", AgentTaskStatus.Completed);
        Assert.Equal("Failed",    AgentTaskStatus.Failed);
    }

    [Fact]
    public void AgentType_Constants_AreCorrect()
    {
        Assert.Equal("Coordinator", NexusAI.Domain.Enums.AgentType.Coordinator);
        Assert.Equal("Document",    NexusAI.Domain.Enums.AgentType.Document);
        Assert.Equal("WebSearch",   NexusAI.Domain.Enums.AgentType.WebSearch);
        Assert.Equal("Analysis",    NexusAI.Domain.Enums.AgentType.Analysis);
        Assert.Equal("Report",      NexusAI.Domain.Enums.AgentType.Report);
    }
}
