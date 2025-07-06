using A2Adotnet.Client;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

using Agent2Agent.AgentA.Plugins;

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
builder.Services.AddSingleton<KernelPluginCollection>((serviceProvider) => 
    [
        KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<ChatResponderAgentPlugin>())
    ]
);

builder.Services.AddTransient(sp =>
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

// Add A2AClient with configuration
builder.Services.AddA2AClient(options =>
{
    options.BaseAddress = new Uri(builder.Configuration["Agents:ChatResponderAgent"] ?? string.Empty);
});

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();

await app.RunAsync();
