using System.Text;
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

        public AgentController(ILogger<AgentController> logger)
        {
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Post([FromBody] string message, [FromServices] ChatCompletionAgent agent)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Message cannot be empty.");
            }

            var response = new StringBuilder();
            await foreach (var result in agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, message), cancellationToken: HttpContext.RequestAborted))
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
            
            return Ok(response);
        }
    }
}
