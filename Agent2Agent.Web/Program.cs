using Agent2Agent.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSignalR();

// Add HttpClient for AgentA service
builder.Services.AddHttpClient("AgentA",(provider, client )=>
{
    var config = provider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["AgentA"]);
});

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
app.MapHub<ChatHub>("/chathub"); // Map SignalR hub

app.Run();
