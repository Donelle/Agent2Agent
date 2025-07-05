using System.ComponentModel;

using A2Adotnet.Client;
using A2Adotnet.Common.Models;
using Microsoft.SemanticKernel;

namespace Agent2Agent.AgentB.Plugins;

/// <summary>
/// KnowledgeGraphAgentPlugin is a plugin for querying the Knowledge Graph Agent (AgentC).
/// It uses the A2AClient to send queries to the Knowledge Graph Agent and receive responses
/// </summary>
public class KnowledgeGraphAgentPlugin
{
    private readonly IA2AClient _client;
    private readonly ILogger<KnowledgeGraphAgentPlugin> _logger;

    public KnowledgeGraphAgentPlugin(IA2AClient client, ILogger<KnowledgeGraphAgentPlugin> logger)
    {
        _client = client;
        _logger = logger;
    }

    [KernelFunction("query_knowledgebase")]
    [Description("Queries the Knowledge Graph for vehicle information based on the provided query string.")]
    public async Task<string> QueryKnowledgeGraphAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchMessage = new Message { Role = "user", Parts = new List<Part> { new TextPart(query) } };
            _logger.LogInformation("Asking knowledg graph agent with query: {Query}", searchMessage);

            var result = await _client.SendTaskAsync(Guid.NewGuid().ToString(), searchMessage, cancellationToken: cancellationToken);
            if (result.Status.State == TaskState.Completed)
            {
                var content = result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)";
                _logger.LogInformation("knowledge graph agent task completed successfully. Result: {Result}", content);

                return content;
            }
            else
            {
                _logger.LogWarning("Knowledge graph agent task did not complete successfully. State: {State}, Message: {Message}",
                    result.Status.State,
                    result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)");
            }
        }
        catch (A2AClientException ex)
        {
            _logger.LogError(ex, "Error while asking knowledge graph agent: {Message}", ex.Message);
        }

        return "Sorry, I couldn't find any information related to your query.";
    }
}
