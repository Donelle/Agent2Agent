using System.ComponentModel;

using A2Adotnet.Client;
using A2Adotnet.Common.Models;
using Microsoft.SemanticKernel;

namespace Agent2Agent.AgentB.Agents;

internal class InternetSearchAgent
{
  	private readonly IA2AClient _a2aClient;
  	private readonly ILogger<InternetSearchAgent> _logger;

	public InternetSearchAgent([FromKeyedServices(Agent2AgentManager.InternetAgentName)] IA2AClient a2aClient, ILogger<InternetSearchAgent> logger)
	{
		_logger = logger;
		_a2aClient = a2aClient;
	}

	[KernelFunction("search_internet")]
	[Description("Search the internet for vehicle registration information.")]
	public async Task<string?> AskWebAgent(
		[Description("The question to search the internet for vehicle registration information")] string query, CancellationToken cancellationToken = default)
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
