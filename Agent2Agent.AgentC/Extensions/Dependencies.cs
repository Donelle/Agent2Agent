using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Implementations;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using StackExchange.Redis;

namespace Agent2Agent.AgentC.Extensions;

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
        services.AddScoped<A2Adotnet.Server.Abstractions.ITaskManager, A2Adotnet.Server.Implementations.InMemoryTaskManager>();

        services.AddA2AServer(options =>
        {
            configuration.GetSection("AgentCard").Bind(options);
        });

        services.AddScoped<IAgentLogicInvoker, KnowledgeGraphAgentLogic>();
        services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();
        services.AddSingleton<IVectorStoreProvider, RedisVectorStoreProvider>();
        services.AddSingleton<FactStorePlugin>();
        services.AddSingleton((serviceProvider) =>
            new KernelPluginCollection
            {
                KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<FactStorePlugin>())
            }
        );

        services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "KnowledgeGraphAgent",
                Instructions = """
                    You are Knowledge Graph Agent (AgentC), a helpful knowledge graph assistant.
                    You can answer questions and provide information about various topics based on the user's input.
                    Use the `search_knowledgebase` function to find relevant information in the knowledge base.
                    After you receive a result from the `search_knowledgebase` function, summarize and format the response into bullet points.
                    If you don't know the answer do not make things up.
                """,
                Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
                Arguments = new KernelArguments (new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            }
        );

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

        var serviceProvider = services.BuildServiceProvider();
        var vectorStoreProvider = serviceProvider.GetRequiredService<IVectorStoreProvider>() as RedisVectorStoreProvider;
        if (vectorStoreProvider != null)
        {
            vectorStoreProvider.EnsureIndexExistsAsync().GetAwaiter().GetResult();
        }
    }
}
