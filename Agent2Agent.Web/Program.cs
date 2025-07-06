using Agent2Agent.Web;
using Agent2Agent.Web.Components;
using Agent2Agent.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();



builder.Services.AddOutputCache();

// Add SignalR services
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

app.Run();
