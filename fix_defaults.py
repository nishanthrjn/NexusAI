content = open("src/NexusAI.Core/Agents/CoordinatorAgent.cs", "r", encoding="utf-8").read()

old = """    private List<AgentTask> GetDefaultTasks(Guid sessionId) =>
        new()
        {
            new() { SessionId=sessionId, AgentType=AgentTypes.WebSearch,
                Title="Research", Description="Research the topic",
                Order=1, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Analysis,
                Title="Analysis", Description="Analyse the findings",
                Order=2, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Report,
                Title="Report", Description="Produce the final report",
                Order=3, Status=AgentTaskStatus.Pending }
        };"""

new = """    private List<AgentTask> GetDefaultTasks(Guid sessionId, string userPrompt = "") =>
        new()
        {
            new() { SessionId=sessionId, AgentType=AgentTypes.WebSearch,
                Title="Research", Description=$"Research information about: {userPrompt}",
                Order=1, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Analysis,
                Title="Analysis", Description=$"Analyse findings related to: {userPrompt}",
                Order=2, Status=AgentTaskStatus.Pending },
            new() { SessionId=sessionId, AgentType=AgentTypes.Report,
                Title="Report", Description=$"Produce a comprehensive report answering: {userPrompt}",
                Order=3, Status=AgentTaskStatus.Pending }
        };"""

# Also update the call sites
content = content.replace(
    'return GetDefaultTasks(session.Id);',
    'return GetDefaultTasks(session.Id, session.UserPrompt);'
)

content = content.replace(old, new)
open("src/NexusAI.Core/Agents/CoordinatorAgent.cs", "w", encoding="utf-8").write(content)
print("Done")
