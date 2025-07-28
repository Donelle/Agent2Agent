using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

namespace Agent2Agent.AgentB.Services;

internal class ChatResponderAgentLogic : IAgentLogicInvoker
{
	private readonly ITaskManager _taskManager;
	private readonly ILogger<ChatResponderAgentLogic> _logger;
	private readonly InProcessRuntime _kernelRuntime;
	private readonly GroupChatOrchestration _orchestration;

	public ChatResponderAgentLogic(ITaskManager taskManager, GroupChatOrchestration orchestration, InProcessRuntime runtime, ILogger<ChatResponderAgentLogic> logger)
	{
		_taskManager = taskManager;
		_logger = logger;
		_kernelRuntime = runtime;
		_orchestration = orchestration;
	}

	public async System.Threading.Tasks.Task ProcessTaskAsync(A2Adotnet.Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, null, cancellationToken);
		var userInput = triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;

		if (userInput != null)
		{
			var response = await _orchestration.InvokeAsync(userInput, _kernelRuntime, cancellationToken);
			var result = await response.GetValueAsync(cancellationToken: cancellationToken);
			
			var resultArtifact = new Artifact() { Parts = new List<Part> { new TextPart(result ?? "No response") } };
			await _taskManager.AddArtifactAsync(task.Id, resultArtifact, cancellationToken);
		}

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
	}
}