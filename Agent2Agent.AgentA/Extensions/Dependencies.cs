using A2Adotnet.Client;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Agent2Agent.AgentA.Plugins;
using Microsoft.Extensions.Http.Resilience;

namespace Agent2Agent.AgentA.Extensions;

public static class Dependencies
{
    public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
    {
		    services.AddProblemDetails();
        services.AddOpenApi();
        services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
        services.AddHttpClient();
        services.AddControllers();
        services.AddOpenAIChatCompletion(
            modelId: configuration["OpenAI:ModelId"] ?? string.Empty,
            apiKey: configuration["OpenAI:ApiKey"] ?? string.Empty
        );

#pragma warning disable EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
		    services.AddHttpClient("A2AClient", (provider, client) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            client.Timeout = TimeSpan.FromSeconds(120); 
        })
			    .RemoveAllResilienceHandlers()
					.AddStandardResilienceHandler(options => {
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
#pragma warning restore EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

		    // Add A2AClient with configuration
		    services.AddSingleton<IA2AClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var options = Options.Create(new A2AClientOptions
            {
                BaseAddress = new Uri(configuration["Agents:ChatResponderAgent"] ?? string.Empty)
            });

					  return new A2AClient(httpClientFactory.CreateClient("A2AClient"), options);
        });
                
        services.AddSingleton<ChatResponderAgentPlugin>();
		    services.AddTransient(sp =>
					  new ChatCompletionAgent
            {
                Name = "RegistrationAdvocate",
                Instructions = """
                    You are Registration Advocate (AgentA), a helpful vehicle registration assistant. 
                    You can answer questions and provide information based on the user's input.
                    Use the {{$respond_to_chat}} to respond to the user.
                    Refuse to answer all user questions that are not related to vehicle registration or vehicles in general.
                """,
                Kernel = new Kernel(sp, [KernelPluginFactory.CreateFromObject(sp.GetRequiredService<ChatResponderAgentPlugin>(), "respond_to_chat")]),
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
						}
        );
    }
}