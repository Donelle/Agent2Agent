using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentB.Agents;


/// <summary>
/// Manages the orchestration of the chat between the Knowledge Graph Agent (AgentC) 
/// and the Internet Search Agent (AgentD).
/// </summary>
/// <remarks>
/// The group chat manager's methods are called in a specific order for each round of the conversation:
///		1. ShouldRequestUserInput: Checks if user (human) input is required before the next agent speaks. 
///		   If true, the orchestration pauses for user input. The user input is then added to the chat history 
///		   of the manager and sent to all agents.
///		2. ShouldTerminate: Determines if the group chat should end (for example, if a maximum number of rounds 
///		   is reached or a custom condition is met). If true, the orchestration proceeds to result filtering.
///		3. FilterResults: Called only if the chat is terminating, to summarize or process the final results of the conversation.
///		4. SelectNextAgent: If the chat is not terminating, selects the next agent to respond in the conversation.
/// </remarks>
internal class Agent2AgentManager : GroupChatManager
{
	public const string KnowledgeAgentName = "KnowledgeGraphAgent";
	public const string InternetAgentName = "InternetSearchAgent";

	public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
	{
		var summary = "No response was found from the assistant";
		var lastMessage = history.LastOrDefault(a => a.Role == AuthorRole.Assistant);

		return lastMessage != null
			? ValueTask.FromResult(new GroupChatManagerResult<string>(lastMessage.Content ?? summary))
			: ValueTask.FromResult(new GroupChatManagerResult<string>(summary));
	}

	public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
	{
		var lastMessage = history.Last();
		if (lastMessage.Role == AuthorRole.User)
		{
			// If the last message was from the user, select the Knowledge Graph Agent to respond.
			return ValueTask.FromResult(new GroupChatManagerResult<string>(KnowledgeAgentName)
			{
				Reason = "Selecting Knowledge Graph Agent to provide vehicle registration information."
			});
		}

		var nextAgent = lastMessage.AuthorName == KnowledgeAgentName
			? InternetAgentName
			: KnowledgeAgentName;

		return ValueTask.FromResult(new GroupChatManagerResult<string>(nextAgent)
		{
			Reason = $"Selecting {nextAgent} to respond."
		});
	}

	public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default) =>
		ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "No user input required." });

	public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
	{
		var lastMessage = history.Last();
		if (lastMessage.Role == AuthorRole.User)
			return ValueTask.FromResult(new GroupChatManagerResult<bool>(false));

		if (lastMessage.AuthorName == InternetAgentName)
			return ValueTask.FromResult(new GroupChatManagerResult<bool>(true));

		var terminate = lastMessage.Content != null &&
			(lastMessage.Content.Length == 0 || lastMessage.Content.Contains("couldn't find", StringComparison.OrdinalIgnoreCase));

		return ValueTask.FromResult(new GroupChatManagerResult<bool>(terminate));
	}
}

