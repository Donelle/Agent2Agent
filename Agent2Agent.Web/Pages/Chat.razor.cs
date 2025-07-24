using Agent2Agent.Web.Service;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;



namespace Agent2Agent.Web.Pages;

public partial class Chat
{
    enum MesageType
    {
        User,
        Agent
    }

    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IChatAgentService ChatAgentService { get; set; } = default!;

    private List<KeyValuePair<MesageType, string>> messages = new();
    private string currentMessage = string.Empty;
    private bool isProcessing = false;

	  private async Task HandleKeyPress(KeyboardEventArgs e)
	  {
		  if (e.Key == "Enter" && !e.ShiftKey)
		  {
			  await SendMessage();
		  }
	  }

	  private async Task SendMessage()
    {
        if (!string.IsNullOrEmpty(currentMessage))
        {
            var messageToSend = currentMessage;
            messages.Add(new(MesageType.User, messageToSend));
            currentMessage = string.Empty;
            isProcessing = true; // Start "thinking"
            StateHasChanged();

            try
            {
                var response = await ChatAgentService.SendMessageAsync(messageToSend);
                messages.Add(new(MesageType.Agent, response ?? string.Empty));
            }
            catch (Exception ex)
            {
                messages.Add(new(MesageType.Agent, $"Error: {ex.Message}"));
            }
            finally
            {
                isProcessing = false; // Stop "thinking"
                StateHasChanged();
            }
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