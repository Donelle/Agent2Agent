public enum AgentRegistryState
{
	/// <summary>
	/// Agent is not registered.
	/// </summary>
	NotRegistered = 0,
	/// <summary>
	/// Agent is registered and ready to receive tasks.
	/// </summary>
	Registered = 1,
	/// <summary>
	/// Agent registration failed.
	/// </summary>
	RegistrationFailed = 2
}

public enum AgentRegistryAction
{
	/// <summary>
	/// Register the agent.
	/// </summary>
	Register = 0,
	/// <summary>
	/// Unregister the agent.
	/// </summary>
	Unregister = 1,
	/// <summary>
	/// Update the agent details.
	/// </summary>
	Update = 2
}

/// <summary>
///  Agent registration message
/// </summary>
public record AgentRegistryMessage(
	AgentRegistryAction Action, 
	AgentDetails AgentDetail,
	AgentNotification? AgentNotification = null
);


/// <summary>
/// Represents a notification containing the current state of the agent registry and details about the agents.
/// </summary>
/// <param name="State">The current state of the agents in the registry.</param>
/// <param name="Agents">An array of <see cref="AgentDetails"/> objects representing the agents in the registry.</param>
public record AgentRegistryNotification(AgentRegistryState State, AgentDetails[] Agents);


/// <summary>
/// Represents the details of an agent.
/// </summary>
public class AgentDetails
{
	public string Name { get; set; } = string.Empty;
	
	public string Description { get; set; } = string.Empty;
	
	public string Uri { get; set; } = string.Empty;

	public string Version { get; set; } = string.Empty;

	public List<AgentSkillDetail> Skills { get; set; } = new();
}

/// <summary>
/// Represents a skill of an agent.
/// </summary>
public class AgentSkillDetail
{
	public string Id { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public List<string> Prompts { get; set; } = new();
}


/// <summary>
/// Configuration for setting up push notifications for task updates.
/// </summary>
public class AgentNotification
{
	/// <summary>
	/// Push Notification ID - created by server to support multiple callbacks.
	/// </summary>
	public string? Id { get; set; }

	/// <summary>
	/// URL for sending the push notifications.
	/// </summary>
	public string Uri { get; set; } = string.Empty;

	/// <summary>
	/// Token unique to this task/session.
	/// </summary>
	public string? Token { get; set; }
}