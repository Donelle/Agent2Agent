using System;
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
    private readonly ChatCompletionAgent _agent;

    public KnowledgeGraphAgentLogic(ILogger<KnowledgeGraphAgentLogic> logger, ITaskManager taskManager, ChatCompletionAgent agent)
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

        if (userInput != null)
        {
            var response = new StringBuilder();
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
            
            if(response.Length > 0)
            {
              var resultArtifact = new Artifact() { Parts = new List<Part> { new TextPart(response.ToString()) } };
              await _taskManager.AddArtifactAsync(task.Id, resultArtifact, cancellationToken);
            }
        }

        _logger.LogInformation("Task {TaskId} completed.", task.Id);
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
    }

}
