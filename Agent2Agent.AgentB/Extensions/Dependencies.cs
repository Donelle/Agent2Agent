using A2Adotnet.Client;
using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Implementations;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;


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
        services.AddScoped<A2Adotnet.Server.Abstractions.ITaskManager, A2Adotnet.Server.Implementations.InMemoryTaskManager>();

        services.AddA2AServer(options =>
        {
            configuration.GetSection("AgentCard").Bind(options);
        });

        // Add A2AClient with configuration
        foreach (var agentName in new[] { "KnowledgeGraphAgent", "InternetSearchAgent" })
        {
            services.AddKeyedScoped<IA2AClient>(agentName, (sp, _) =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var options = Options.Create(new A2AClientOptions
                {
                    BaseAddress = new Uri(configuration[$"Agents:{agentName}"] ?? string.Empty),
                });

                return new A2AClient(httpClientFactory.CreateClient(agentName), options);
            });
        }
        
        services.AddScoped<KnowledgeGraphAgentPlugin>();
        services.AddScoped<InternetSearchAgentPlugin>();
        services.AddScoped<IAgentLogicInvoker, ChatResponderAgentLogic>();
        services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "VehicleRegistrationAssistant",
                Instructions = """
                    You are Vehicle Registration Assistant (AgentB), a helpful vehicle registration assistant. 
                    You can answer questions and provide information about vehicle registration and related topics based on the user's input.
                    Ask the Knowledge Graph Agent (AgentC) first for help using the `ask_knowledge_graph_agent` function.
                    If you don't know the answer, you can ask Internet Search Agent (AgentD) for help, however do not make things up. 
                    If you need to ask Internet Search Agent (AgentD), use the `ask_internet_search_agent` function.
                    Refuse to answer all user questions that are not related to vehicle registration or vehicles in general.
                """,
                Kernel = new Kernel(sp,
                [
                    KernelPluginFactory.CreateFromObject(sp.GetRequiredService<KnowledgeGraphAgentPlugin>()),
                    KernelPluginFactory.CreateFromObject(sp.GetRequiredService<InternetSearchAgentPlugin>())
                ])
            }
        );
    }
}
