using A2A;

using Microsoft.Extensions.Http.Resilience;

using Agent2Agent.AgentB.Agents;
using Agent2Agent.AgentB.Configurations;

namespace Agent2Agent.AgentB.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));

		services.Configure<A2AClientOptions>(options =>
		{
			configuration.GetSection("AgentCard").Bind(options);
		});

		// Add A2AClient with configuration
		foreach (var agentName in new[] { Agent2AgentManager.KnowledgeAgentName, Agent2AgentManager.InternetAgentName })
		{
			SetResiliencePolicies(services, agentName);

			services.AddKeyedSingleton<IA2AClient>(agentName, (sp, _) =>
			{
				var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
				return new A2AClient(new Uri(configuration[$"Agents:{agentName}"] ?? string.Empty), 
					httpClientFactory.CreateClient(agentName));
			});
		}

		services.AddSingleton<IAgentLogicInvoker, ChatResponderAgentLogic>();
		services.AddSingleton<ITaskManager>(sp =>
		{
			var taskManager = new TaskManager();
			var agent = sp.GetRequiredService<IAgentLogicInvoker>();
			agent.Attach(taskManager);
			return taskManager;
		});

		services.AddTransient<KnowledgeBaseAgent>();
		services.AddTransient<InternetSearchAgent>();
		services.AddTransient<Agent2AgentManager>();
	}

	static void SetResiliencePolicies(IServiceCollection services, string agentName)
	{
		services.AddHttpClient(agentName, (provider, client) =>
		{
			var config = provider.GetRequiredService<IConfiguration>();
			client.Timeout = TimeSpan.FromSeconds(120);
			client.BaseAddress = new Uri(config[$"Agents:{agentName}"] ?? string.Empty);
		})
		.RemoveAllResilienceHandlers()
		.AddStandardResilienceHandler(options =>
		{
			options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
			{
				SamplingDuration = TimeSpan.FromSeconds(240),
				Name = $"{agentName}CircuitBreaker"
			};
			options.AttemptTimeout = new HttpTimeoutStrategyOptions
			{
				Timeout = TimeSpan.FromSeconds(120),
				Name = $"{agentName}AttemptTimeout",
			};
			options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
			{
				Timeout = TimeSpan.FromSeconds(120),
				Name = $"{agentName}Timeout"
			};
		});
	}
}
