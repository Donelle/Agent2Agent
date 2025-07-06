using A2Adotnet.Server;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDependencies();

var app = builder.Build();

app.UseRouting();
app.MapA2AWellKnown();
app.MapA2AEndpoint();

await app.RunAsync();
