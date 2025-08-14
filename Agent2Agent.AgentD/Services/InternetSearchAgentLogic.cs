using A2A;

using Agent2Agent.AgentD.Configurations;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentD.Services;

public interface IAgentLogicInvoker
{
	void Attach(ITaskManager taskManager);
}

internal class InternetSearchAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<InternetSearchAgentLogic> _logger;
	private readonly ChatCompletionAgent _agent;
	private readonly A2AClientOptions _clientOptions;
	private ITaskManager _taskManager = null!;

	public InternetSearchAgentLogic(
		ILogger<InternetSearchAgentLogic> logger, 
		ChatCompletionAgent agent, 
		IOptions<A2AClientOptions> clientOptions)
	{
		_logger = logger;
		_agent = agent;
		_clientOptions = clientOptions.Value;
	}

	public void Attach(ITaskManager taskManager)
	{
		_taskManager = taskManager;
		taskManager.OnTaskCreated = ExecuteAgentTaskAsync;
		taskManager.OnAgentCardQuery = GetAgentCardAsync;
	}

	private Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken token) =>
	 token.IsCancellationRequested
			? Task.FromCanceled<AgentCard>(token)
			: Task.FromResult(new AgentCard
			{
				Name = _clientOptions.Name,
				Description = _clientOptions.Description,
				Url = _clientOptions.Url,
				Version = _clientOptions.Version,
				DefaultInputModes = _clientOptions.DefaultInputModes.ToList(),
				DefaultOutputModes = _clientOptions.DefaultOutputModes.ToList(),
				Provider = new AgentProvider
				{
					Organization = _clientOptions.Provider.Organization,
				},
				Capabilities = new AgentCapabilities
				{
					Streaming = _clientOptions.Capabilities.Streaming,
					PushNotifications = _clientOptions.Capabilities.PushNotifications,
				},
				Skills = _clientOptions.Skills.Select(a => new AgentSkill
				{
					Id = a.Id,
					Name = a.Name,
					Description = a.Description,
					Examples = a.Examples.ToList(),
				}).ToList(),
			});

	public async Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken token)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, null, cancellationToken: token);
		var userInput = task.History?.Last().Parts.First().AsTextPart().Text;
		var response = new StringBuilder();

		try
		{
			_logger.LogInformation("Searching the internet for query: {UserInput}", userInput);
			await foreach (var result in _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, userInput), cancellationToken: token))
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

		var message = new Message { MessageId = Guid.NewGuid().ToString(), Role = MessageRole.Agent, Parts = [new TextPart { Text = response.ToString() }] };
		await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed, message, cancellationToken: token);

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
	}

}
