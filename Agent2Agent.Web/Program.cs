using Agent2Agent.Web;
using Agent2Agent.Web.Components;
using Agent2Agent.Web.Hubs;
using Microsoft.Extensions.FileProviders; // Add namespace for ChatHub

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

/*
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
*/
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSignalR();

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
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Components")),
    RequestPath = "/Components"
});

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();
/*
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
*/
app.MapRazorPages();
app.MapBlazorHub();
app.MapDefaultEndpoints();
app.MapFallbackToPage("/_Host");

app.MapHub<ChatHub>("/chathub"); // Map SignalR hub

app.Run();
