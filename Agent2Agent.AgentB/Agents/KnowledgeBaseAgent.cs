using System.ComponentModel;

using A2Adotnet.Client;
using A2Adotnet.Common.Models;

using Agent2Agent.AgentB.Agents;

namespace Agent2Agent.AgentB.Plugins;

/// <summary>
/// KnowledgeGraphAgentPlugin is a plugin for querying the Knowledge Graph Agent (AgentC).
/// It uses the A2AClient to send queries to the Knowledge Graph Agent and receive responses
/// </summary>
public class KnowledgeBaseAgent : IAgent
{
	private readonly IA2AClient _client;
	private readonly ILogger<KnowledgeBaseAgent> _logger;

	public KnowledgeBaseAgent([FromKeyedServices(Agent2AgentManager.KnowledgeAgentName)] IA2AClient client, ILogger<KnowledgeBaseAgent> logger)
	{
		_client = client;
		_logger = logger;
	}

	[Description("Search the knowledge base for vehicle registration information.")]
	public async Task<string> InvokeAsync(string userInput, CancellationToken cancellationToken)
	{
		var content = "Sorry, I couldn't find any information related to your query.";
		try
		{
			var searchMessage = new Message { Role = "User", Parts = new List<Part> { new TextPart(userInput) } };
			_logger.LogInformation("Asking knowledg graph agent with query: {@Query}", searchMessage);

			var result = await _client.SendTaskAsync(Guid.NewGuid().ToString(), searchMessage, cancellationToken: cancellationToken);
			if (result.Status.State == TaskState.Completed)
			{
				_logger.LogInformation("knowledge graph agent task completed successfully. Result: {Result}", content);
				content = result.Artifacts?
					.SelectMany(a => a.Parts)
					.OfType<TextPart>()
					.Select(p => p.Text)
					.FirstOrDefault() ?? content;
			}
			else
			{
				_logger.LogWarning("Knowledge graph agent task did not complete successfully. State: {State}, Message: {Message}",
						result.Status.State, content);
			}
		}
		catch (A2AClientException ex)
		{
			_logger.LogError(ex, "Error while asking knowledge graph agent: {Message}", ex.Message);
		}

		return content;
	}
}
