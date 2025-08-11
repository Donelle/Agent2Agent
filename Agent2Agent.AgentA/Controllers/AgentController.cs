using Agent2Agent.AgentA.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;


namespace Agent2Agent.AgentA.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AgentController : ControllerBase
	{
		private readonly ILogger<AgentController> _logger;
		private readonly IConversationService _conversationService;

		public AgentController(ILogger<AgentController> logger, IConversationService service)
		{
			_logger = logger;
			_conversationService = service;
		}

		[HttpPost("chat")]
		public async Task<IActionResult> Post([FromBody] ChatMessage message, [FromServices] ChatCompletionAgent agent)
		{
			if (message == null || string.IsNullOrWhiteSpace(message?.Content))
			{
				return BadRequest("Message cannot be empty.");
			}

			var response = await _conversationService.SendMessageAsync(
				new ChatMessageContent(AuthorRole.User, message.Content) { AuthorName = message.SessionId }, agent, HttpContext.RequestAborted);

			return !string.IsNullOrEmpty(response)
				? Ok(response)
				: BadRequest("Failed to get a valid response.");
		}
	}

	public record ChatMessage(string SessionId, string Content);
}
