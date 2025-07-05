using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Client;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

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

builder.Services.AddA2AClient(options =>
{
    options.BaseAddress = new Uri(builder.Configuration["Agents:KnowledgeGraphAgent"] ?? string.Empty);
});

builder.Services.AddScoped<IAgentLogicInvoker, ChatResponderAgentLogic>();

builder.Services.AddSingleton<KnowledgeGraphAgentPlugin>();
builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) =>
    [
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<KnowledgeGraphAgentPlugin>())
    ]
);

builder.Services.AddTransient(sp =>
    new ChatCompletionAgent
    {
        Name = "VehicleRegistrationAssistant",
        Instructions = """
            You are Vehicle Registration Assistant (AgentB), a helpful vehicle registration assistant. 
            You can answer questions and provide information about vehicle registration and related topics based on the user's input.
            If you don't know the answer or need more information, you can ask the Knowledge Graph Agent (AgentC) for help, however do not make things up.
            Refuse to answer all user questions that are not related to vehicle registration or vehicles in general.
        """,
        Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
    }
);

var app = builder.Build();

app.UseRouting();
app.MapA2AWellKnown();
app.MapA2AEndpoint();

await app.RunAsync();
