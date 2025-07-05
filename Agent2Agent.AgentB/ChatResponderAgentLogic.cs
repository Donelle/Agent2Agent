using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentB;

internal class ChatResponderAgentLogic : IAgentLogicInvoker
{
    private readonly ITaskManager _taskManager;
    private readonly ILogger<ChatResponderAgentLogic> _logger;
    private readonly ChatCompletionAgent _agent ;

    public ChatResponderAgentLogic(ITaskManager taskManager, ILogger<ChatResponderAgentLogic> logger, ChatCompletionAgent chatCompletionAgent)
    {
        _taskManager = taskManager;
        _logger = logger;
        _agent = chatCompletionAgent;
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