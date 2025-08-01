using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentC;

internal class KnowledgeGraphAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<KnowledgeGraphAgentLogic> _logger;
	private readonly ITaskManager _taskManager;
	private readonly FactStoreService _factStore;
	private readonly ChatCompletionAgent _agent;
	public KnowledgeGraphAgentLogic(ILogger<KnowledgeGraphAgentLogic> logger, ITaskManager taskManager, FactStoreService factStore, ChatCompletionAgent agent)
	{
		_logger = logger;
		_taskManager = taskManager;
		_factStore = factStore;
		_agent = agent;
	}

	public async System.Threading.Tasks.Task ProcessTaskAsync(A2Adotnet.Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, null, cancellationToken);
		var userInput = triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;
		var response = new StringBuilder();

		try
		{
			var result = await _factStore.SearchKnowledgeBaseAsync(userInput ?? string.Empty, cancellationToken);
			if (result.Count > 0)
			{
				response.AppendLine("Here are some relevant pieces of information I found:");

				var prompt = new StringBuilder($"{userInput}\n\nUse the following information as reference for this inquiry:\n\n");
				foreach (var item in result)
					prompt.Append(item);

				await foreach (var agentResult in _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt.ToString()), cancellationToken: cancellationToken))
				{
					if (agentResult.Message is ChatMessageContent chatResponse)
					{
						response.Append(chatResponse.Content);
					}
					else
					{
						// Handle other types of results if necessary
						_logger.LogWarning("Received unexpected message type: {MessageType}", agentResult.Message.GetType());
					}
				}
			}
			else
			{
				response.AppendLine("I couldn't find any relevant information in the knowledge base.");
			}
		} 
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while processing task: {TaskId}", task.Id);
			response.Append("An error occurred while processing your request. Please try again later.");
		}

		var message = new Message() { Role = "Assistant", Parts = new List<Part> { new TextPart(response.ToString()) } };

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, message, cancellationToken);
	}
}
