namespace Agent2Agent.AgentB.Configurations;

internal class A2AClientOptions
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public ProviderOptions Provider { get; set; } = new ProviderOptions();
	public CapabilitiesOptions Capabilities { get; set; } = new CapabilitiesOptions();
	public string[] DefaultInputModes { get; set; } = Array.Empty<string>();
	public string[] DefaultOutputModes { get; set; } = Array.Empty<string>();
	public SkillOptions[] Skills { get; set; } = Array.Empty<SkillOptions>();

	public class ProviderOptions
	{
		public string Organization { get; set; } = string.Empty;
	}

	public class CapabilitiesOptions
	{
		public bool Streaming { get; set; }
		public bool PushNotifications { get; set; }
	}

	public class SkillOptions
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string[] Examples { get; set; } = Array.Empty<string>();
	}
}