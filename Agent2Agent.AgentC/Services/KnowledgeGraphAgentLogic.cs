using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

namespace Agent2Agent.AgentC;

internal class KnowledgeGraphAgentLogic : IAgentLogicInvoker
{
	private readonly ILogger<KnowledgeGraphAgentLogic> _logger;
	private readonly ITaskManager _taskManager;
	private readonly FactStorePlugin _plugin;

	public KnowledgeGraphAgentLogic(ILogger<KnowledgeGraphAgentLogic> logger, ITaskManager taskManager, FactStorePlugin plugin)
	{
		_logger = logger;
		_taskManager = taskManager;
		_plugin = plugin;
	}

	public async System.Threading.Tasks.Task ProcessTaskAsync(A2Adotnet.Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing task: {TaskId}", task.Id);

		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, null, cancellationToken);
		var userInput = triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;
		var response = new StringBuilder();

		try
		{
			var result = await _plugin.SearchKnowledgeBaseAsync(userInput ?? string.Empty, cancellationToken);
			if (result.Count > 0)
			{
				response.AppendLine("Here are some relevant pieces of information I found:");
				foreach (var item in result)
					response.Append(item);
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

		var resultArtifact = new Artifact() { Parts = new List<Part> { new TextPart(response.ToString()) } };
		await _taskManager.AddArtifactAsync(task.Id, resultArtifact, cancellationToken);

		_logger.LogInformation("Task {TaskId} completed.", task.Id);
		await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
	}

}
