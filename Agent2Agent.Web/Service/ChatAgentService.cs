using System.Text;
using System.Text.Json;

namespace Agent2Agent.Web.Service;

public interface IChatAgentService
{
		Task<string> SendMessageAsync(string sessionId, string message);
}

internal class ChatAgentService : IChatAgentService
{
	public record ChatMessage(string SessionId, string Content);

	HttpClient _httpClient;
	ILogger<ChatAgentService> _logger;

	public ChatAgentService(HttpClient client, ILogger<ChatAgentService> logger)
	{
		_httpClient = client;
		_logger = logger;
	}

	public async Task<string> SendMessageAsync(string sessionId, string content)
	{
		try
		{
			// Call AgentA's chat endpoint
			var jsonContent = new StringContent(
					JsonSerializer.Serialize(new ChatMessage(sessionId, content)),
					Encoding.UTF8,
					"application/json"
			);

			var response = await _httpClient.PostAsync("api/agent/chat", jsonContent);
			if (response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				return responseContent ?? string.Empty;
			}
			else
			{
				_logger.LogError($"Error: Failed to get response from agent. Status: {response.StatusCode} Message: {response.RequestMessage}");
				return $"Error: Failed to get response from agent. Status: {response.StatusCode} Message: {response.RequestMessage}";
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error: {ex.Message}");
			return $"Error: {ex.Message}";
		}
	}
}
