using System.ComponentModel;

using A2Adotnet.Client;
using A2Adotnet.Common.Models;
using Microsoft.SemanticKernel;

namespace Agent2Agent.AgentB.Plugins;

internal class InternetSearchAgentPlugin
{
  	private readonly IA2AClient _a2aClient;
  	private readonly ILogger<InternetSearchAgentPlugin> _logger;

	public InternetSearchAgentPlugin([FromKeyedServices("InternetSearchAgent")] IA2AClient a2aClient, ILogger<InternetSearchAgentPlugin> logger)
	{
		_logger = logger;
		_a2aClient = a2aClient;
	}

	[KernelFunction("ask_internet_search_agent")]
	[Description("Asks the Internet Search Agent (AgentD) to search the web for information related to the user's query.")]
	public async Task<string?> AskWebAgent(string query, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			_logger.LogWarning("Received empty query. Returning null.");
			return null;
		}

		try
		{
			var searchMessage = new Message { Role = "user", Parts = new List<Part> { new TextPart(query) } };
			_logger.LogInformation("Asking web agent with query: {Query}", searchMessage);

			var result = await _a2aClient.SendTaskAsync(Guid.NewGuid().ToString(), searchMessage, cancellationToken: cancellationToken);
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
		catch (A2AClientException ex)
		{
			_logger.LogError(ex, "Error while asking web agent: {Message}", ex.Message);
		}

		return null;
	}
}
