using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using StackExchange.Redis;

namespace Agent2Agent.AgentC.Extensions;

public static class Dependencies
{
    public static void AddDependencies(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
        services.AddHttpClient();
        services.AddOpenAIChatCompletion(
            modelId: services.BuildServiceProvider().GetRequiredService<IConfiguration>()["OpenAI:ModelId"] ?? string.Empty,
            apiKey: services.BuildServiceProvider().GetRequiredService<IConfiguration>()["OpenAI:ApiKey"] ?? string.Empty
        );

        services.AddA2AServer(options =>
        {
            services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetSection("AgentCard").Bind(options);
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
            var redisConnectionString = sp.GetRequiredService<IConfiguration>()["Redis:ConnectionString"];
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
