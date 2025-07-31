using A2Adotnet.Client;
using A2Adotnet.Common.Models;

using Microsoft.AspNetCore.Mvc;


namespace Agent2Agent.AgentA.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AgentController : ControllerBase
	{
		private readonly ILogger<AgentController> _logger;
		private readonly IA2AClient _a2aClient;

		public AgentController(ILogger<AgentController> logger, IA2AClient a2aClient)
		{
			_logger = logger;
			_a2aClient = a2aClient;
		}

		[HttpPost("chat")]
		public async Task<IActionResult> Post([FromBody] string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return BadRequest("Message cannot be empty.");
			}

			var response = string.Empty;

			try
			{
				var chatMessage = new Message { Role = "user", Parts = new List<Part> { new TextPart(message) } };
				_logger.LogInformation("Responding to chat message: {Message}", chatMessage);


				var result = await _a2aClient.SendTaskAsync(Guid.NewGuid().ToString(), chatMessage, cancellationToken: HttpContext.RequestAborted);
				response = result.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)";

				if (result.Status.State == TaskState.Completed)
				{
					_logger.LogInformation("Task completed successfully. Result: {Result}", response);
				}
				else
				{ 
					_logger.LogWarning("Task did not complete successfully. State: {State}, Message: {Message}", result.Status.State, response);
				}
			}
			catch (A2AClientException ex)
			{
				_logger.LogError(ex, "Task failed with A2A Error Code {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Task failed with unexpected error.");
			}

			return Ok(response);
		}

		[HttpPost("send-message")]
		public IActionResult SendMessage([FromBody] ChatMessage message)
		{
			// Forward the message to the chat logic
			// Example: ChatLogic.HandleMessage(message);
			return Ok(new { Status = "Message received" });
		}

		[HttpPost("upload-file")]
		public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			var filePath = Path.Combine("UploadedFiles", file.FileName);

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			// Notify chat participants about the uploaded file
			// Example: ChatLogic.NotifyFileUpload(file.FileName);

			return Ok(new { Status = "File uploaded successfully", FileName = file.FileName });
		}

		public record ChatMessage(string User, string Content);
	}
}
