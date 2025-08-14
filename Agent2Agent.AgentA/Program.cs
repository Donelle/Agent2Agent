using Agent2Agent.AgentA.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAgentRegistration();

builder.Services.AddAgentDependencies(builder.Configuration);

var app = builder.Build();

app.UseRouting();
app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();

