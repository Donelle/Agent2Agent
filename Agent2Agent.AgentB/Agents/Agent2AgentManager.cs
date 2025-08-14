using System.Collections.Concurrent;
using System.Text.Json;

namespace Agent2Agent.AgentB.Agents;

internal class Agent2AgentManager
{
	private readonly ILogger<Agent2AgentManager> _logger;
	private readonly ConcurrentDictionary<string, RegisteredAgent> _agents;
	private readonly IHttpClientFactory _factory;

	public Agent2AgentManager(IHttpClientFactory factory, ILogger<Agent2AgentManager> logger)
	{
		_logger = logger;
		_agents = new();
		_factory = factory;
	}

	/// <summary>
	/// Processes the specified input message and returns the result.
	/// </summary>
	/// <param name="input">The input message to be processed. Cannot be null or empty.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. If the operation is canceled, the method will terminate early.</param>
	/// <returns>A string representing the processed result of the input message. The exact format and content of the result        
	/// depend on the processing logic.</returns>
	public string ProcessMessage(string input, CancellationToken cancellationToken)
	{
		var message = JsonSerializer.Deserialize<AgentRegistryMessage>(input);
		if (message == null)
		{
			_logger.LogInformation("Message is invalid");
			return "Message is invalid";
		}

		return message.Action switch
		{
			AgentRegistryAction.Register => Register(message.AgentDetail, message.AgentNotification, cancellationToken),
			AgentRegistryAction.Unregister => Unregister(message.AgentDetail.Name, cancellationToken),
			_ => "Unknown state"
		};
	}

	private string Unregister(string name, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(name))
		{
			_logger.LogInformation("Agent name is missing.");
			return "Agent name is required.";
		}

		if (_agents.ContainsKey(name))
		{
			_agents.TryRemove(name, out RegisteredAgent agent);
			if (agent != null)
			{
				_ = Task.Run(async () =>
				{
					try
					{
						// Notify all registered agents of the new agent
						var agents = _agents.Values.ToArray();
						foreach (var agent in agents)
							await agent.InvokeAsync(AgentRegistryState.NotRegistered, [agent.Details], cancellationToken);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error occurred in the notification task.");
					}
				}, cancellationToken);
			}

			_logger.LogInformation("Unregistered agent {Name}.", name);
			return $"Unregistered agent {name}.";
		}

		_logger.LogInformation("Agent with name {Name} does not exist.", name);
		return $"Agent with name {name} does not exist.";
	}

	private string Register(AgentDetails agentDetails, AgentNotification? notification, CancellationToken cancellationToken)
	{
		if (agentDetails == null || string.IsNullOrEmpty(agentDetails.Name))
		{
			_logger.LogInformation("Agent name is missing.");
			return "Agent name is required.";
		}

		if (_agents.ContainsKey(agentDetails.Name))
		{
			_logger.LogInformation("Agent with name {Name} already exists.", agentDetails.Name);
			return $"Agent with name {agentDetails.Name} already exists.";
		}

		var newAgent = new RegisteredAgent(agentDetails, notification, _factory.CreateClient(), _logger);
		_ = Task.Run(async () =>
		{
			try
			{
				// Notify all registered agents of the new agent
				var agents = _agents.Values.ToArray();
				for(int i = 0; i < agents.Length; i++)
				{
					if (cancellationToken.IsCancellationRequested)
						return;
					// Notify existing agents of the new agent
					await agents[i].InvokeAsync(AgentRegistryState.Registered, [ newAgent.Details ], cancellationToken);
				}

				// Notify the new agent of existing agents
				await newAgent.InvokeAsync(AgentRegistryState.Registered, agents.Select(a => a.Details).ToArray(), cancellationToken);

				_agents.TryAdd(agentDetails.Name, newAgent);
				_logger.LogInformation("Registered agent {Name}.", agentDetails.Name);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred in the notification task.");
			}
		}, cancellationToken);

		return "Notifications processed";
	}
}

