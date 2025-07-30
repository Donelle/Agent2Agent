using A2Adotnet.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddAgentDependencies(builder.Configuration);

var app = builder.Build();

app.UseRouting();
app.MapA2AWellKnown();
app.MapA2AEndpoint();
app.MapDefaultEndpoints();

await app.RunAsync();