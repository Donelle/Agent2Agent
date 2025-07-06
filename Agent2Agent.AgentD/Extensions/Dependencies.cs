using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Implementations;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Agent2Agent.AgentD.Extensions;

public static class Dependencies
{
    public static IServiceCollection AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IAgentLogicInvoker, InternetSearchAgentLogic>();
        services.AddSingleton<SearchPlugin>();
        services.AddSingleton((serviceProvider) =>
            new KernelPluginCollection
            {
                KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<SearchPlugin>())
            }
        );

        services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "InternetSearchAgent",
                Instructions = """
                    You are Internet Search Agent, a helpful internet search assistant.
                    You can find information on the internet based on the user's input.
                    You will use the `search_internet` function first to search for relevant information.
                    After you receive a result from the `search_internet` function, perform any necessary
                    instructions returned by the function (if applicable).
                """,
                Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
                Arguments = new KernelArguments (new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            }
        );

        return services;
    }
}
