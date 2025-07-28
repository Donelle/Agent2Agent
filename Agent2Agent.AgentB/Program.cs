using A2Adotnet.Server;

using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddAgentDependencies(builder.Configuration);

var runtime = new InProcessRuntime();
await runtime.StartAsync();

builder.Services.AddSingleton(runtime);

var app = builder.Build();

app.UseRouting();
app.MapA2AWellKnown();
app.MapA2AEndpoint();
app.MapDefaultEndpoints();

await app.RunAsync();
await runtime.StopAsync();