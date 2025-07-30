using A2Adotnet.Client;
using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;

using Agent2Agent.AgentB.Agents;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Agent2Agent.AgentB.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
		
		// Override the singleton ITaskManager registration with scoped to fix DI issue
		services.AddScoped<ITaskManager, A2Adotnet.Server.Implementations.InMemoryTaskManager>();

		services.AddA2AServer(options =>
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
				var options = Options.Create(new A2AClientOptions
				{
					BaseAddress = new Uri(configuration[$"Agents:{agentName}"] ?? string.Empty),
				});

				return new A2AClient(httpClientFactory.CreateClient(agentName), options);
			});
		}

		services.AddTransient<KnowledgeBaseAgent>();
		services.AddTransient<InternetSearchAgent>();
		services.AddTransient<Agent2AgentManager>();
		services.AddScoped<IAgentLogicInvoker, ChatResponderAgentLogic>();
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
