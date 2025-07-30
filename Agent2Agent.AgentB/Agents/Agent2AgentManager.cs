using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentB.Agents;


/// <summary>
/// Manages the orchestration of the chat between the Knowledge Graph Agent (AgentC) 
/// and the Internet Search Agent (AgentD).
/// </summary>
/// <remarks>
/// The group chat manager's methods are called in a specific order for each round of the conversation:
///		1. ShouldTerminate: Determines if the group chat should end based on the following conditions:
///		- If the last message is from the user, the chat continues.
///		- If the last message is from the Internet Agent, the chat can terminate.
///		- If the last message indicates that the Knowledge Graph Agent did find information, the chat can terminate.
///		- If the last message indicates that the Knowledge Graph Agent did not find any information, the chat continues.
///		2. FilterResults: Called only if the chat is terminating, to summarize or process the final results of the conversation.
///		3. SelectNextAgent: If the chat is not terminating, selects the next agent to respond in the conversation by the following logic:
///		- If the last message was from the user, the Knowledge Graph Agent is selected to respond.
///		- If the last message was from the Knowledge Graph Agent, the Internet Search Agent is selected to respond.
/// </remarks>
internal class Agent2AgentManager
{
	public const string KnowledgeAgentName = "KnowledgeGraphAgent";
	public const string InternetAgentName = "InternetSearchAgent";

	private readonly ILogger<Agent2AgentManager> _logger;
	private readonly Dictionary<string, IAgent> _agents; 

	public Agent2AgentManager(KnowledgeBaseAgent knowledgeBaseAgent, InternetSearchAgent internetSearchAgent, ILogger<Agent2AgentManager> logger)
	{
		_logger = logger;
		_agents = new Dictionary<string, IAgent>
		{
			{ KnowledgeAgentName, knowledgeBaseAgent },
			{ InternetAgentName, internetSearchAgent }
		};
	}

	public async Task<string> InvokeAsync(string userInput, CancellationToken cancellationToken)
	{
		ChatHistory history = new ChatHistory(userInput, AuthorRole.User);

		for (int i = 0; i <= 2; i++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation("Operation cancelled by user.");
				return string.Empty;
			}

			if (ShouldTerminate(history))
			{
				_logger.LogInformation("Terminating chat as per logic.");
				return FilterResults(history);
			}

			var nextAgentName = SelectNextAgent(history);
			var result = await _agents[nextAgentName].InvokeAsync(userInput, cancellationToken);
			history.Add(new ChatMessageContent(AuthorRole.Assistant, result) { AuthorName = nextAgentName });
		}

		return "No response was found from the assistant";
	}

	public string FilterResults(ChatHistory history)
	{
		var summary = "No response was found from the assistant";
		var lastMessage = history.LastOrDefault(a => a.Role == AuthorRole.Assistant);
		return lastMessage?.Content ?? summary;
	}

	public string SelectNextAgent(ChatHistory history)
	{
		var lastMessage = history.Last();
		if (lastMessage.Role == AuthorRole.User)
		{
			// If the last message was from the user, select the Knowledge Graph Agent to respond.
			return KnowledgeAgentName;
		}

		return lastMessage.AuthorName == KnowledgeAgentName
			? InternetAgentName
			: KnowledgeAgentName;
	}

	public bool ShouldTerminate(ChatHistory history)
	{
		// If the last message is from the user, we do not terminate yet we 
		// wait for the next agent's response.
		var lastMessage = history.Last();
		if (lastMessage.Role == AuthorRole.User)
			return false;

		// if the last message is from the Internet Agent we can terminate the chat
		if (lastMessage.AuthorName == InternetAgentName)
			return true;

		// Check to see if the last message indicates that the Knowledge Graph Agent did not find any information.
		var notFoundMessage = "I couldn't find any relevant information in the knowledge base.";
		return !lastMessage.Content?.Contains(notFoundMessage, StringComparison.OrdinalIgnoreCase) ?? false;
	}
}

