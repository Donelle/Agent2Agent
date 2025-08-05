using A2A;
using A2A.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Register dependencies 
builder.Services.AddAgentDependencies(builder.Configuration);

var app = builder.Build();
var taskManager = app.Services.GetRequiredService<ITaskManager>();

app.UseRouting();
app.MapA2A(taskManager, "/a2a");
app.MapDefaultEndpoints();

await app.RunAsync();
