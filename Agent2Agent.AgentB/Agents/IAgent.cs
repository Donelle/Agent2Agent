namespace Agent2Agent.AgentB.Agents;


/// <summary>
/// Represents a conversational agent.
/// </summary>
public interface IAgent
{
	/// <summary>
	/// Invokes the agent with the given user input.
	/// </summary>
	Task<string> InvokeAsync(string userInput, CancellationToken cancellationToken);
}
