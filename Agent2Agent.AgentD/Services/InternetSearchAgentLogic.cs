using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentD.Services;

public class InternetSearchAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<InternetSearchAgentLogic> _logger;
	private readonly ITaskManager _taskManager;
	private readonly ChatCompletionAgent _agent;

	public InternetSearchAgentLogic(ILogger<InternetSearchAgentLogic> logger, ITaskManager taskManager, ChatCompletionAgent agent)
	{
		_logger = logger;
		_taskManager = taskManager;
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
			await foreach (var result in _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, userInput), cancellationToken: cancellationToken))
			{
				if (result.Message is ChatMessageContent chatResponse)
				{
					response.Append(chatResponse.Content);
				}
				else
				{
					// Handle other types of results if necessary
					_logger.LogWarning("Received unexpected message type: {MessageType}", result.Message.GetType());
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while processing task: {TaskId}", task.Id);
			response.Append("An error occurred while processing your request. Please try again later.");
		}

		if (response.Length == 0)
			response.Append("Sorry, I couldn't find any information related to your query.");

		var message = new Message { Role = "Assistant",  Parts = new List<Part> { new TextPart(response.ToString()) } };

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, message, cancellationToken);
	}

}
