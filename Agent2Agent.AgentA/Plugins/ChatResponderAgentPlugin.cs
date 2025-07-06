using System.ComponentModel;

using A2Adotnet.Client;
using A2Adotnet.Common.Models;

using Microsoft.SemanticKernel;


namespace Agent2Agent.AgentA.Plugins;

/// <summary>
/// ChatResponderAgentPlugin is a plugin for responding to chat messages.
/// It uses the A2AClient to send messages to Chat Responder Agent (AgentB) and receive responses.
/// This plugin is used by AgentA to respond to user messages.
/// </summary>
internal class ChatResponderAgentPlugin
{
    private readonly IA2AClient _a2aClient;
    private readonly ILogger<ChatResponderAgentPlugin> _logger;

    public ChatResponderAgentPlugin(IA2AClient a2aClient, ILogger<ChatResponderAgentPlugin> logger)
    {
        _logger = logger;
        _a2aClient = a2aClient;
    }

    [KernelFunction("respond_to_chat")]
    [Description("Responds to a chat message and returns a ChatResponse object. This function is used by AgentA to respond to user messages.")]
    public async Task<string?> RespondToChat(string message, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Received empty message. Returning null.");
            return null;
        }


        try
        {
            var chatMessage = new Message { Role = "user", Parts = new List<Part> { new TextPart(message) } };
            _logger.LogInformation("Responding to chat message: {Message}", chatMessage);


            var result = await _a2aClient.SendTaskAsync(Guid.NewGuid().ToString(), chatMessage, cancellationToken: cancellationToken);
            if (result.Status.State == TaskState.Completed)
            {
                var content = result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)";
                _logger.LogInformation("Task completed successfully. Result: {Result}", content);

                return content;
            }
            else
            {
                _logger.LogWarning("Task did not complete successfully. State: {State}, Message: {Message}",
                    result.Status.State,
                    result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)");
            }
        }
        catch (A2AClientException ex)
        {
            _logger.LogError(ex, "{ExampleName} failed with A2A Error Code {ErrorCode}: {ErrorMessage}", nameof(RespondToChat), ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExampleName} failed with unexpected error.", nameof(RespondToChat));
        }

        return null;
    }
}
