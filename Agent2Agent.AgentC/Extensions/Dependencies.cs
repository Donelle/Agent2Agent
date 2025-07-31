using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Implementations;

using StackExchange.Redis;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Agent2Agent.AgentC.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
		services.AddHttpClient();
	
		// Override the singleton ITaskManager registration with scoped to fix DI issue
		services.AddScoped<ITaskManager, InMemoryTaskManager>();
		services.AddScoped<IAgentLogicInvoker, KnowledgeGraphAgentLogic>();
		services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();
		services.AddSingleton<IVectorStoreProvider, RedisVectorStoreProvider>();
		services.AddTransient<FactStoreService>();
		services.AddSingleton(sp =>
		{
			var redisConnectionString = configuration["Redis:ConnectionString"];
			if (string.IsNullOrEmpty(redisConnectionString))
			{
				throw new InvalidOperationException("Redis connection string is not configured.");
			}

			var redis = ConnectionMultiplexer.Connect(redisConnectionString);
			if (!redis.IsConnected)
			{
				throw new InvalidOperationException("Could not connect to Redis.");
			}

			return redis;
		});

		services.AddA2AServer(options =>
		{
			configuration.GetSection("AgentCard").Bind(options);
		});

		var serviceProvider = services.BuildServiceProvider();
		var vectorStoreProvider = serviceProvider.GetRequiredService<IVectorStoreProvider>() as RedisVectorStoreProvider;
		if (vectorStoreProvider != null)
		{
			vectorStoreProvider.EnsureIndexExistsAsync().GetAwaiter().GetResult();
		}

		services.AddTransient(sp =>
				new ChatCompletionAgent
				{
					Name = "KnowledgebaseAgent",
					Instructions = """
                You are Knowledgebase Agent, a helpful knowledge base assistant.
                You will find information in the knowledge base based on the user's input.
            """,
					Kernel = Kernel.CreateBuilder()
						.AddOpenAIChatCompletion(configuration["OpenAI:ModelId"] ?? string.Empty, configuration["OpenAI:ApiKey"] ?? string.Empty)
						.Build()
				}
		);
	}
}
