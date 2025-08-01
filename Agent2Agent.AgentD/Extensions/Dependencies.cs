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
                    You will find information on the internet based on the user's input.
                    Include relevant details and sources in your response.
                """,
                Kernel = Kernel.CreateBuilder()
						    .AddOpenAIChatCompletion(configuration["OpenAI:ModelId"] ?? string.Empty, configuration["OpenAI:ApiKey"] ?? string.Empty)
						    .Build()
						}
        );

        return services;
    }
}
