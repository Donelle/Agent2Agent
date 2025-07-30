using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

using Agent2Agent.AgentB.Agents;

namespace Agent2Agent.AgentB.Services;

internal class ChatResponderAgentLogic : IAgentLogicInvoker
{
	private readonly ITaskManager _taskManager;
	private readonly ILogger<ChatResponderAgentLogic> _logger;
	private readonly Agent2AgentManager _orchestration;

	public ChatResponderAgentLogic(ITaskManager taskManager, Agent2AgentManager orchestration, ILogger<ChatResponderAgentLogic> logger)
	{
		_taskManager = taskManager;
		_logger = logger;
		_orchestration = orchestration;
	}

	public async System.Threading.Tasks.Task ProcessTaskAsync(A2Adotnet.Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, null, cancellationToken);
		var userInput = triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;

		if (userInput != null)
		{
			var response = await _orchestration.InvokeAsync(userInput, cancellationToken);
			var resultArtifact = new Artifact() { Parts = new List<Part> { new TextPart(response ?? "No response") } };

			await _taskManager.AddArtifactAsync(task.Id, resultArtifact, cancellationToken);
		}

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
	}
}