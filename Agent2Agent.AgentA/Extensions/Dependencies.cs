using A2A;

using Agent2Agent.AgentA.Plugins;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
			return new A2AClient(new Uri(configuration["Agents:OrchestratorAgent"] ?? string.Empty),
				httpClientFactory.CreateClient("A2AClient"));
		});

		services.AddSingleton<CustomerAdvocatePlugin>();

		services.AddTransient(sp =>
		{
			var kernel = Kernel.CreateBuilder()
					.AddOpenAIChatCompletion(
						configuration["OpenAI:ModelId"] ?? string.Empty, 
						configuration["OpenAI:ApiKey"] ?? string.Empty
			);
			kernel.Plugins.AddFromObject(sp.GetRequiredService<CustomerAdvocatePlugin>(), "CustomerAdvocate");

			return new ChatCompletionAgent
			{
				Name = "CustomerAdvocateAssistant",
				Instructions = """
            You are Customer Advocate Agent, a customer service assistant.
            Your role is to assist customers with their automotive inquiries.
            You will provide information about automotive products, services, registrations, and policies.
            Always validate that inquiries are related to automotive topics before proceeding.
            Once validated, forward the entire inquiry to the answer_vehicle_inquiry function for processing.
            Do not provide any other information if it is not related to automotive inquiries.
            All other information is out of your scope and you should not answer it.
            """,
				Kernel = kernel.Build(),
				 Arguments = new KernelArguments(new OpenAIPromptExecutionSettings { 
					 FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
				 })
			};
		});
	}
}