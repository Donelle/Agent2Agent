using Agent2Agent.Web.Service;
using Microsoft.Extensions.Http.Resilience;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazorBootstrap();
builder.Services.AddOutputCache();
builder.Services.AddSignalR(o =>
{
  o.ClientTimeoutInterval = TimeSpan.FromSeconds(120); 
  o.EnableDetailedErrors = true;
});

 // Add HttpClient for AgentA service
 builder.Services.AddHttpClient<IChatAgentService, ChatAgentService>((provider, client) =>
 {
 	var config = provider.GetRequiredService<IConfiguration>();
 	client.BaseAddress = new Uri(config["AgentA"] ?? string.Empty);
 	client.Timeout = TimeSpan.FromSeconds(120);
 })
 .RemoveAllResilienceHandlers()
 .AddStandardResilienceHandler(options =>
 {
 	options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
 	{
 		SamplingDuration = TimeSpan.FromSeconds(240),
 		Name = "WebAppCircuitBreaker"
 	};
 	options.AttemptTimeout = new HttpTimeoutStrategyOptions
 	{
 		Timeout = TimeSpan.FromSeconds(120),
 		Name = "WebAppAttemptTimeout",
 	};
 	options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
 	{
 		Timeout = TimeSpan.FromSeconds(120),
 		Name = "WebAppTimeout"
 	};
 	options.Retry = new HttpRetryStrategyOptions
 	{
 		MaxRetryAttempts = 2,
 		Delay = TimeSpan.FromSeconds(2),
 		Name = "WebAppRetry",
 	};
 });
 
 // In-memory chat history for the lifetime of the app
 builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();


var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();
app.MapRazorPages();
app.MapBlazorHub();
app.MapDefaultEndpoints();
app.MapFallbackToPage("/_Host");
app.Run();
