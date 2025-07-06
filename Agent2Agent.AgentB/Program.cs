using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Client;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddHttpClient();
builder.Services.AddOpenAIChatCompletion(
    modelId: builder.Configuration["OpenAI:ModelId"] ?? string.Empty,
    apiKey: builder.Configuration["OpenAI:ApiKey"] ?? string.Empty
);

builder.Services.AddA2AServer(options =>
{
    builder.Configuration.GetSection("AgentCard").Bind(options);
});


builder.Services.AddScoped<IAgentLogicInvoker, ChatResponderAgentLogic>();

builder.Services.AddSingleton<KnowledgeGraphAgentPlugin>();
builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) =>
    [
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<KnowledgeGraphAgentPlugin>()),
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<InternetSearchAgentPlugin>())
    ]
);

builder.Services.AddTransient(sp =>
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
        Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
    }
);

// Add A2AClient with configuration
foreach (var agentName in new[] { "KnowledgeGraphAgent", "InternetSearchAgent"})
{
    builder.Services.TryAddKeyedTransient<IA2AClient>(agentName, (sp, _) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var options = Options.Create(new A2AClientOptions
        {
            BaseAddress = new Uri(config[$"Agents:{agentName}"] ?? string.Empty),             
        });

        return new A2AClient(httpClientFactory.CreateClient(agentName), options);
    });
};

var app = builder.Build();

app.UseRouting();
app.MapA2AWellKnown();
app.MapA2AEndpoint();

await app.RunAsync();
