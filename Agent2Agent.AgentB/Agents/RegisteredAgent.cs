using System.Text.Json;

using Polly;

namespace Agent2Agent.AgentB.Agents;


internal class RegisteredAgent
{
	private readonly AgentDetails _details;
	private readonly AgentNotification? _notification;
	private readonly HttpClient _httpClient;
	private readonly ILogger _logger;

	public RegisteredAgent(AgentDetails details, AgentNotification? notification, HttpClient httpClient, ILogger logger)
	{
		_details = details;
		_httpClient = httpClient;
		_logger = logger;
		_notification = notification;
	}

	public AgentDetails Details => _details;

	/// <summary>
	/// Invokes the agent with the given user input.
	/// </summary>
	public async Task InvokeAsync(AgentRegistryState state, AgentDetails [] details, CancellationToken cancellationToken)
	{
		if (_notification == null || string.IsNullOrEmpty(_notification.Uri))
		{
			_logger.LogWarning("Notification URL is not set for agent {Name}. Not sending notification", _details.Name);
			return;
		}

		try
		{
			var retryPolicy = Policy
				.Handle<HttpRequestException>()
				.OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
					(outcome, timespan, retryAttempt, context) =>
					{
						_logger.LogWarning("Retrying notification to agent {Name}. Attempt {RetryAttempt}", _details.Name, retryAttempt);
					});

			var jsonPayload = JsonSerializer.Serialize(new AgentRegistryNotification(state, details));
			var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

			_logger.LogInformation("Sending notification to agent {Name}", _details.Name);

			var response = await retryPolicy.ExecuteAsync(async () => await _httpClient.PostAsync(_notification.Uri, content, cancellationToken));
			if (response.IsSuccessStatusCode)
			{
				_logger.LogInformation("Notification sent to agent {Name} successfully.", _details.Name);
			}
			else
			{
				_logger.LogWarning("Failed to send notification to agent {Name}. Status Code: {StatusCode}", _details.Name, response.StatusCode);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while sending notification to agent {Name}.", _details.Name);
		}
	}
}