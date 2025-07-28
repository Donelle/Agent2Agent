using A2Adotnet.Client;
using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;

using Agent2Agent.AgentB.Agents;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace Agent2Agent.AgentB.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
		services.AddHttpClient();
		services.AddOpenAIChatCompletion(
				modelId: configuration["OpenAI:ModelId"] ?? string.Empty,
				apiKey: configuration["OpenAI:ApiKey"] ?? string.Empty
		);

		// Override the singleton ITaskManager registration with scoped to fix DI issue
		services.AddScoped<ITaskManager, A2Adotnet.Server.Implementations.InMemoryTaskManager>();

		services.AddA2AServer(options =>
		{
			configuration.GetSection("AgentCard").Bind(options);
		});

		// Add A2AClient with configuration
		foreach (var agentName in new[] { Agent2AgentManager.KnowledgeAgentName, Agent2AgentManager.InternetAgentName })
		{
			RegisterHttpClient(services, configuration, agentName);
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

		services.AddTransient(p =>
		{
			var knowledgeGraphAgent = new ChatCompletionAgent
			{
				Name = Agent2AgentManager.KnowledgeAgentName,
				Description = "Knowledge Graph Agent (AgentC) is a helpful assistant with the knowledge for vehicle information.",
				Instructions = """
			 You are Knowledge Graph Agent (AgentC), a helpful assistant with the knowledge of vehicle registration information.
			 You can answer questions and provide information about vehicle registration and related topics.
			 """,
				Kernel = new Kernel(p, [KernelPluginFactory.CreateFromType<KnowledgeGraphAgent>(Agent2AgentManager.KnowledgeAgentName, p) ]),
				Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { 
					FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
				})
			};

			var internetSearchAgent = new ChatCompletionAgent
			{
				Name = Agent2AgentManager.InternetAgentName,
				Description = "Internet Search Agent (AgentD) is a helpful assistant for searching the internet.",
				Instructions = """
				You are Internet Search Agent (AgentD), a helpful assistant for searching the internet.
				You can search the internet to answer questions and provide information about vehicle registration.
				""",
				Kernel = new Kernel(p, [KernelPluginFactory.CreateFromType<InternetSearchAgent>(Agent2AgentManager.InternetAgentName, p)]),
				Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
			};


			return new GroupChatOrchestration(new Agent2AgentManager { MaximumInvocationCount = 2 }, knowledgeGraphAgent, internetSearchAgent);
		});

		services.AddScoped<IAgentLogicInvoker, ChatResponderAgentLogic>();
	}

	static void RegisterHttpClient(IServiceCollection services, IConfiguration configuration, string agentName)
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
