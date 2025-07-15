using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace Agent2Agent.Web.Pages;

public partial class Chat
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    private List<string> messages = new List<string>();
    private string currentMessage = string.Empty;
    private bool isProcessing = false;


    protected override async Task OnInitializedAsync()
    {
    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrEmpty(currentMessage))
        {
            currentMessage = string.Empty;
            isProcessing = true; // Start "thinking"
            StateHasChanged();

            // Simulate server call
            await Task.Delay(2000); // Replace with actual server call

            isProcessing = false; // Stop "thinking"
            StateHasChanged();
        }
    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[file.Size];
            await stream.ReadAsync(buffer);           
        }
    }
}