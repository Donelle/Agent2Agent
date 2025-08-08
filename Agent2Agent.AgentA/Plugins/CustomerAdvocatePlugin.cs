using System.ComponentModel;

using A2A;

using Microsoft.SemanticKernel;

namespace Agent2Agent.AgentA.Plugins;

internal class CustomerAdvocatePlugin(IA2AClient client, ILogger<CustomerAdvocatePlugin> logger)
{

	[KernelFunction("answer_vehicle_inquiry")]
	[Description("Primary handler for all automotive-related inquiries, including support, products, services, and vehicle registration. This function should be called for any prompt related to automotive topics.")]
	public async Task<string> FetchAsync(
		[Description("The vehicle inquiry")] string inquiry, CancellationToken cancellationToken)
	{
		var response = string.Empty;

		try
		{
			var chatMessage = new Message
			{
				MessageId = Guid.NewGuid().ToString(),
				Role = MessageRole.User,
				Parts = [new TextPart { Text = inquiry }]
			};

			logger.LogInformation("Sending chat message: {@Message}", chatMessage);


			var result = (AgentTask)await client.SendMessageAsync(new() { Message = chatMessage }, cancellationToken);
			if (result.Status.State == TaskState.Completed)
			{
				response = result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)";
				logger.LogInformation("Task completed successfully. Result: {Result}", response);
			}
			else
			{
				logger.LogWarning("Task did not complete successfully. State: {State}, Message: {Message}", result.Status.State, response);
			}
		}
		catch (A2AException ex)
		{
			logger.LogError(ex, "Task failed with A2A Error Code {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);
		}

		return response;
	}
}
