using A2A;

namespace Agent2Agent.AgentB.Agents;

internal class InternetSearchAgent : IAgent
{
  	private readonly IA2AClient _a2aClient;
  	private readonly ILogger<InternetSearchAgent> _logger;

	public InternetSearchAgent([FromKeyedServices(Agent2AgentManager.InternetAgentName)] IA2AClient a2aClient, ILogger<InternetSearchAgent> logger)
	{
		_logger = logger;
		_a2aClient = a2aClient;
	}

	/// <summary>
	/// "Search the internet for vehicle registration information."
	/// </summary>
	/// <param name="userInput">The user's query.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns> The search results as a string.</returns>
	public async Task<string> InvokeAsync(string userInput, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(userInput))
		{
			_logger.LogWarning("Received empty query. Returning null.");
			return string.Empty;
		}

		try
		{
			var searchMessage = new Message { 
				MessageId = Guid.NewGuid().ToString(),
				Role = MessageRole.User, 
				Parts = [new TextPart {  Text = userInput } ] 
			};
			_logger.LogInformation("Asking web agent with query: {@Query}", searchMessage);

			var result = (AgentTask)await _a2aClient.SendMessageAsync(new() { Message = searchMessage }, cancellationToken);
			if (result.Status.State == TaskState.Completed)
			{
				var content = result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)";
				_logger.LogInformation("Web agent task completed successfully. Result: {Result}", content);

				return content;
			}
			else
			{
				_logger.LogWarning("Web agent task did not complete successfully. State: {State}, Message: {Message}",
					result.Status.State,
					result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)");
			}
		}
		catch (A2AException ex)
		{
			_logger.LogError(ex, "Error while asking web agent: {Message}", ex.Message);
		}

		return string.Empty;
	}
}
