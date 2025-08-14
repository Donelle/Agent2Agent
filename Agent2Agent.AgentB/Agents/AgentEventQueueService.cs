using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Agent2Agent.AgentB.Agents;

public enum AgentEventType
{
    Register,
    Unregister
}

public record AgentEvent(AgentEventType Type, AgentDetails? Details, AgentNotification? Notification);

public class AgentEventQueueService : BackgroundService
{
    private readonly Channel<AgentEvent> _eventChannel;
    private readonly ConcurrentDictionary<string, RegisteredAgent> _agents = new();
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<AgentEventQueueService> _logger;

    public AgentEventQueueService(
        Channel<AgentEvent> eventChannel,
        IHttpClientFactory factory,
        ILogger<AgentEventQueueService> logger)
    {
        _eventChannel = eventChannel;
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(stoppingToken))
        {
            switch (evt.Type)
            {
                case AgentEventType.Register:
                    await HandleRegisterAsync(evt.Details!, evt.Notification, stoppingToken);
                    break;
                case AgentEventType.Unregister:
                    await HandleUnregisterAsync(evt.Details!.Name, stoppingToken);
                    break;
            }
        }
    }

    private async Task HandleRegisterAsync(AgentDetails agentDetails, AgentNotification? notification, CancellationToken cancellationToken)
    {
        if (agentDetails == null || string.IsNullOrEmpty(agentDetails.Name))
        {
            _logger.LogInformation("Agent name is missing.");
            return;
        }

        if (_agents.ContainsKey(agentDetails.Name))
        {
            _logger.LogInformation("Agent with name {Name} already exists.", agentDetails.Name);
            return;
        }

        var newAgent = new RegisteredAgent(agentDetails, notification, _factory.CreateClient(), _logger);

        // Notify all registered agents of the new agent
        var agents = _agents.Values.ToArray();
        foreach (var agent in agents)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            await agent.InvokeAsync(AgentRegistryState.Registered, [ newAgent.Details ], cancellationToken);
        }

        // Notify the new agent of existing agents
        await newAgent.InvokeAsync(AgentRegistryState.Registered, agents.Select(a => a.Details).ToArray(), cancellationToken);

        _agents.TryAdd(agentDetails.Name, newAgent);
        _logger.LogInformation("Registered agent {Name}.", agentDetails.Name);
    }

    private async Task HandleUnregisterAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogInformation("Agent name is missing.");
            return;
        }

        if (_agents.TryRemove(name, out RegisteredAgent? agent))
        {
            // Notify all registered agents of the unregistration
            var agents = _agents.Values.ToArray();
            foreach (var a in agents)
            {
                await a.InvokeAsync(AgentRegistryState.NotRegistered, [ agent.Details ], cancellationToken);
            }
            _logger.LogInformation("Unregistered agent {Name}.", name);
        }
        else
        {
            _logger.LogInformation("Agent with name {Name} does not exist.", name);
        }
    }
}