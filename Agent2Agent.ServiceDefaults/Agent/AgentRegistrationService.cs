using System.Text.Json;

using A2A;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


public sealed class AgentRegistrationService : BackgroundService
{
	private readonly AgentCard _opts;
	private readonly ILogger<AgentRegistrationService> _logger;
	private readonly IA2AClient _a2AClient;

	public AgentRegistrationService(IA2AClient a2AClient, IOptions<AgentCard> agentInfoOptions, ILogger<AgentRegistrationService> logger)
	{
		_opts = agentInfoOptions.Value;
		_logger = logger;
		_a2AClient = a2AClient;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await ProcessAgentRegistryActionAsync(AgentRegistryAction.Register, stoppingToken);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await ProcessAgentRegistryActionAsync(AgentRegistryAction.Unregister, cancellationToken);
	}

	private async Task ProcessAgentRegistryActionAsync(AgentRegistryAction action, CancellationToken cancellationToken)
	{
		try
		{
			var content = JsonSerializer.Serialize(_opts.ToRegistryMessage(action));
			var chatMessage = new Message
			{
				MessageId = Guid.NewGuid().ToString(),
				Role = MessageRole.User,
				Parts = [new TextPart { Text = content }]
			};

			var result = (AgentTask)await _a2AClient.SendMessageAsync(new() { Message = chatMessage });
			if (result.Status.State == TaskState.Completed)
			{
				_logger.LogInformation("Agent {Action} completed successfully", action);
			}
			else
			{
				_logger.LogWarning("Agent {Action} did not complete successfully. State: {State}", action, result.Status.State);
			}
		}
		catch (A2AException ex)
		{
			_logger.LogError(ex, "Agent {Action} failed with A2A Error Code {ErrorCode}: {ErrorMessage}", action, ex.ErrorCode, ex.Message);
		}
	}
}
