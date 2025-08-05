using A2A;

using Microsoft.Extensions.Http.Resilience;

namespace Agent2Agent.AgentA.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
		services.AddHttpClient();
		services.AddControllers();
	
		services.AddHttpClient("A2AClient", (provider, client) =>
		{
			var config = provider.GetRequiredService<IConfiguration>();
			client.Timeout = TimeSpan.FromSeconds(120);
		})
			.RemoveAllResilienceHandlers()
			.AddStandardResilienceHandler(options =>
			{
				options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
				{
					SamplingDuration = TimeSpan.FromSeconds(240),
					Name = "AgentACircuitBreaker"
				};
				options.AttemptTimeout = new HttpTimeoutStrategyOptions
				{
					Timeout = TimeSpan.FromSeconds(120),
					Name = "AgentAAttemptTimeout",
				};
				options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
				{
					Timeout = TimeSpan.FromSeconds(120),
					Name = "AgentATimeout"
				};
			});

		// Add A2AClient with configuration
		services.AddSingleton<IA2AClient>(sp =>
		{
			var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
			return new A2AClient(new Uri(configuration["Agents:ChatResponderAgent"] ?? string.Empty), 
				httpClientFactory.CreateClient("A2AClient"));
		});
	}
}