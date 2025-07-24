using Agent2Agent.Web.Service;

using Microsoft.Extensions.Http.Resilience;

using Polly;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazorBootstrap();

// Configure SignalR with enhanced options for long connections
builder.Services.AddSignalR(o =>
{
  o.ClientTimeoutInterval = TimeSpan.FromSeconds(120); 
  o.EnableDetailedErrors = true;
});

// Add HttpClient for AgentA service
#pragma warning disable EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHttpClient<IChatAgentService, ChatAgentService>((provider, client) =>
{
	var config = provider.GetRequiredService<IConfiguration>();
	client.BaseAddress = new Uri(config["AgentA"]);
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
#pragma warning restore EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Services.AddOutputCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
