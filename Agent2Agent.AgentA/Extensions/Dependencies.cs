using A2Adotnet.Client;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Agent2Agent.AgentA.Plugins;

namespace Agent2Agent.AgentA.Extensions;

public static class Dependencies
{
    public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
    {   
        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
        services.AddHttpClient();
        services.AddOpenAIChatCompletion(
            modelId: configuration["OpenAI:ModelId"] ?? string.Empty,
            apiKey: configuration["OpenAI:ApiKey"] ?? string.Empty
        );

        // Add A2AClient with configuration
        services.AddA2AClient(options =>
        {
            options.BaseAddress = new Uri(configuration["Agents:ChatResponderAgent"] ?? string.Empty);
        });

        services.AddSingleton<ChatResponderAgentPlugin>();
        services.AddSingleton<KernelPluginCollection>((serviceProvider) =>
            [
                KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<ChatResponderAgentPlugin>())
            ]
        );

        services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "RegistrationAdvocate",
                Instructions = """
                    You are Registration Advocate (AgentA), a helpful vehicle registration assistant. 
                    You can answer questions and provide information based on the user's input.
                    Use the `respond_to_chat` function to respond to the user.
                    Refuse to answer all user questions that are not related to vehicle registration or vehicles in general.
                """,
                Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
            }
        );
    }
}