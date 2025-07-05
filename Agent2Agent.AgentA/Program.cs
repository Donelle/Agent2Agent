using A2Adotnet.Client;
using Agent2Agent.AgentA.Plugins;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddHttpClient();
builder.Services.AddOpenAIChatCompletion(
    modelId: builder.Configuration["OpenAI:ModelId"] ?? string.Empty,
    apiKey: builder.Configuration["OpenAI:ApiKey"] ?? string.Empty
);

builder.Services.AddSingleton<ChatResponderAgentPlugin>();
builder.Services.AddSingleton<InternetSearchAgentPlugin>();
builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) => 
    [
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<ChatResponderAgentPlugin>()),
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<InternetSearchAgentPlugin>())
    ]
);

builder.Services.AddTransient(sp =>
    new ChatCompletionAgent
    {
        Name = "RegistrationAdvocate",
        Instructions = """
            You are RegistrationAdvocate (AgentA), a helpful vehicle registration assistant. 
            You can answer questions and provide information based on the user's input.
            Use the `respond_to_chat` function to respond to the user.
            If you don't know the answer, you can ask Internet Search Agent (AgentD) for help, however do not make things up. 
            If you need to ask Internet Search Agent (AgentD), use the `ask_web_agent` function.
            Refuse to answer all user questions that are not related to vehicle registration or vehicles in general.
        """,
        Kernel = new Kernel(sp, sp.GetRequiredService<KernelPluginCollection>()),
    }
);

// Add A2AClient with configuration
foreach (var agentName in new[] { "ChatResponderAgent", "InternetSearchAgent"})
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

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();

await app.RunAsync();
