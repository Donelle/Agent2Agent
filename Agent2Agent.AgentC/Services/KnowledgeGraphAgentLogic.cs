

using A2A;

using Agent2Agent.AgentC.Configurations;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentC;

public interface IAgentLogicInvoker
{
	void Attach(ITaskManager taskManager);
}

internal class KnowledgeGraphAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<KnowledgeGraphAgentLogic> _logger;
	private readonly FactStoreService _factStore;
	private readonly ChatCompletionAgent _agent;
	private readonly A2AClientOptions _clientOptions;
	private ITaskManager _taskManager = null!;

	public KnowledgeGraphAgentLogic(
		ILogger<KnowledgeGraphAgentLogic> logger, 
		IOptions<A2AClientOptions> options, 
		FactStoreService factStore, 
		ChatCompletionAgent agent)
	{
		_logger = logger;
		_factStore = factStore;
		_agent = agent;
		_clientOptions = options.Value;
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
			var result = await _factStore.SearchKnowledgeBaseAsync(userInput ?? string.Empty, token);
			if (result.Count > 0)
			{
				var prompt = new StringBuilder($"{userInput}\n\nUse the following information as reference for this inquiry:\n\n");
				foreach (var item in result)
					prompt.Append(item);

				await foreach (var agentResult in _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt.ToString()), cancellationToken: token))
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

		var message = new Message { MessageId = Guid.NewGuid().ToString(), Role = MessageRole.Agent, Parts = [new TextPart { Text = response.ToString() }] };
		await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed, message, cancellationToken: token);

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
	}
}
