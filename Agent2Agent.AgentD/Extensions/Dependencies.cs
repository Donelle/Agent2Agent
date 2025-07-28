using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Implementations;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
        services.AddScoped<ITaskManager, InMemoryTaskManager>();

        services.AddA2AServer(options =>
        {
            configuration.GetSection("AgentCard").Bind(options);
        });

        services.AddScoped<IAgentLogicInvoker, InternetSearchAgentLogic>();
  
        services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "InternetSearchAgent",
                Instructions = """
                    You are Internet Search Agent, a helpful internet search assistant.
                    You can find information on the internet based on the user's input.
                """,
                Kernel = new Kernel(sp),
                Arguments = new KernelArguments (new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            }
        );

        return services;
    }
}
