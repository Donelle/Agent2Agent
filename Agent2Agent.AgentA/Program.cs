using Agent2Agent.AgentA.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddAgentDependencies(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();
app.UseRouting();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();

await app.RunAsync();
