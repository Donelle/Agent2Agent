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
		private readonly IAgentCacheProvider _cacheProvider;

		public AgentController(ILogger<AgentController> logger, IConversationService service, IAgentCacheProvider cacheProvider)
		{
			_logger = logger;
			_conversationService = service;
			_cacheProvider = cacheProvider;
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

		[HttpPost("notify")]
		public IActionResult Notify([FromBody] AgentRegistryNotification notification)
		{
			if (notification == null || notification.Agents.Length == 0)
			{
				_logger.LogWarning("Received empty notification or no agents specified. {@Data}", notification);
				return BadRequest("Notification details are required.");
			}

			foreach (var agent in notification.Agents)
			{
				if (notification.State == AgentRegistryState.Registered)
					_cacheProvider.AddAgent(agent);
				else if (notification.State == AgentRegistryState.NotRegistered)
					_cacheProvider.RemoveAgent(agent.Name);
				else if (notification.State == AgentRegistryState.RegistrationFailed)
				{
					_cacheProvider.RemoveAgent(agent.Name); // Remove agent if its in our cache
					_logger.LogWarning("Agent registration failed for {AgentName}", agent.Name);
				}
			}

			return Ok();
		}
	}

	public record ChatMessage(string SessionId, string Content);
}
