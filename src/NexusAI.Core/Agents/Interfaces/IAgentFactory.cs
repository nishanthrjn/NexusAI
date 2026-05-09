using NexusAI.Core.Agents.Interfaces;

namespace NexusAI.Core.Agents.Interfaces;

public interface IAgentFactory
{
    IAgent GetAgent(string agentType);
    IEnumerable<IAgent> GetAllAgents();
}
