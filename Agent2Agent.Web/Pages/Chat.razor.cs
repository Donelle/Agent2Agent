using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;

namespace Agent2Agent.Web.Pages;

public partial class Chat
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    private HubConnection hubConnection = default!;
    private List<string> messages = new List<string>();
    private string currentMessage = string.Empty;
    private bool isTyping;

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/chathub"))
            .Build();

        hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            messages.Add($"{user}: {message}");
            StateHasChanged();
        });

        hubConnection.On<string>("UserTyping", (user) =>
        {
            isTyping = true;
            StateHasChanged();
            Task.Delay(2000).ContinueWith(_ => { isTyping = false; StateHasChanged(); });
        });

        await hubConnection.StartAsync();
    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrEmpty(currentMessage))
        {
            await hubConnection.SendAsync("SendMessage", "User", currentMessage);
            currentMessage = string.Empty;
        }
    }

    private async Task NotifyTyping()
    {
        await hubConnection.SendAsync("NotifyTyping", "User");
    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[file.Size];
            await stream.ReadAsync(buffer);
            await hubConnection.SendAsync("UploadFile", "User", file.Name, buffer);
        }
    }
}