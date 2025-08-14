using System.Collections.Concurrent;

namespace Agent2Agent.AgentA.Services;


public interface IAgentCacheProvider
{
	void AddAgent(AgentDetails agent);

	void RemoveAgent(string name);

	IReadOnlyCollection<AgentDetails> Agents { get; }
}

/// <summary>
/// Provides a mechanism for managing a collection of agents in memory, allowing for the addition, removal, and
/// retrieval of agent details.
/// </summary>
/// <remarks>This class maintains an in-memory cache of agents, identified by their unique names. It is
/// thread-safe and uses a <see cref="ConcurrentDictionary{TKey,TValue}"/>. Logging is performed for key
/// operations such as adding or removing agents. Extend this class to use a shared redis cache if needed in a clustered environment.
/// </remarks>
internal class AgentCacheProvider (ILogger<AgentCacheProvider> _logger) : IAgentCacheProvider
{
    private readonly ConcurrentDictionary<string, AgentDetails> _agents = new();

    public IReadOnlyCollection<AgentDetails> Agents => _agents.Values.ToList(); // Return a copy of the values

    public void AddAgent(AgentDetails agent)
    {
        if (_agents.TryAdd(agent.Name, agent))
        {
            _logger.LogInformation("Added Agent: {Name}", agent.Name);
        }
        else
        {
            _logger.LogWarning("Agent with name {Name} already exists.", agent.Name);
        }
    }

    public void RemoveAgent(string name)
    {
        if (_agents.TryRemove(name, out _))
        {
            _logger.LogInformation("Removed Agent: {Name}", name);
        }
        else
        {
            _logger.LogWarning("Attempted to remove non-existent agent: {Name}", name);
        }
    }
}
