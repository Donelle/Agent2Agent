using A2A;

using Agent2Agent.AgentB.Agents;
using Agent2Agent.AgentB.Configurations;

using Microsoft.Extensions.Options;

namespace Agent2Agent.AgentB.Services;

public interface IAgentLogicInvoker
{
	void Attach(ITaskManager taskManager);
}

internal class RegistryAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<RegistryAgentLogic> _logger;
	private readonly Agent2AgentManager _orchestration;
	private readonly A2AClientOptions _clientOptions;
	private ITaskManager _taskManager = null!;

	public RegistryAgentLogic(
		IOptions<A2AClientOptions> options, 
		Agent2AgentManager orchestration, 
		ILogger<RegistryAgentLogic> logger)
	{
		_logger = logger;
		_orchestration = orchestration;
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


	private async Task ExecuteAgentTaskAsync(AgentTask task, CancellationToken token)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateStatusAsync(task.Id, TaskState.Working, null, cancellationToken: token);
		var input = task.History?.Last().Parts.First().AsTextPart().Text;

		var response = _orchestration.ProcessMessage(input ?? string.Empty, token);
		var result = new Message { Role = MessageRole.Agent, Parts = [new TextPart { Text = response ?? "No response" }] };

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateStatusAsync(task.Id, TaskState.Completed, result, cancellationToken: token);
	}
}