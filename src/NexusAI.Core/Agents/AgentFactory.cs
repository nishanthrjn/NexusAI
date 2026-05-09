using NexusAI.Core.Agents.Interfaces;

namespace NexusAI.Core.Agents;

public class AgentFactory : IAgentFactory
{
    private readonly Dictionary<string, IAgent> _agents;

    public AgentFactory(IEnumerable<IAgent> agents)
    {
        _agents = agents.ToDictionary(a => a.AgentType, a => a);
    }

    public IAgent GetAgent(string agentType)
    {
        if (_agents.TryGetValue(agentType, out var agent))
            return agent;

        throw new InvalidOperationException(
            $"No agent registered for type: {agentType}. " +
            $"Available: {string.Join(", ", _agents.Keys)}");
    }

    public IEnumerable<IAgent> GetAllAgents() => _agents.Values;
}
