using System.Text.Json;
using System.Threading.Channels;

namespace Agent2Agent.AgentB.Agents;

public class AgentRegistryManager
{
    private readonly ILogger<AgentRegistryManager> _logger;
    private readonly Channel<AgentEvent> _eventChannel;

    public AgentRegistryManager(ILogger<AgentRegistryManager> logger, Channel<AgentEvent> eventChannel)
    {
        _logger = logger;
        _eventChannel = eventChannel;
    }

    public string ProcessMessage(string input, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<AgentRegistryMessage>(input);
        if (message == null)
        {
            _logger.LogInformation("Message is invalid");
            return "Message is invalid";
        }

        switch (message.Action)
        {
            case AgentRegistryAction.Register:
                _eventChannel.Writer.TryWrite(new AgentEvent(AgentEventType.Register, message.AgentDetail, message.AgentNotification));
                return "Register event enqueued";
            case AgentRegistryAction.Unregister:
                _eventChannel.Writer.TryWrite(new AgentEvent(AgentEventType.Unregister, message.AgentDetail, null));
                return "Unregister event enqueued";
            default:
                return "Unknown state";
        }
    }
}

