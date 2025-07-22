using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;
using System.Text.Json;


namespace Agent2Agent.Web.Pages;

public partial class Chat
{
    enum MesageType
    {
        User,
        Agent
	  }
   
	  [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private Dictionary<MesageType, string> messages = new ();
    private string currentMessage = string.Empty;
    private bool isProcessing = false;

    private async Task SendMessage()
    {
        if (!string.IsNullOrEmpty(currentMessage))
        {
            var messageToSend = currentMessage;
            messages.Add(MesageType.User, messageToSend);
            currentMessage = string.Empty;
            isProcessing = true; // Start "thinking"
            StateHasChanged();

            try
            {
                // Call AgentA's chat endpoint
                var httpClient = HttpClientFactory.CreateClient("AgentA");
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(messageToSend),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("api/agent/chat", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Remove quotes from the response if it's a JSON string
                    var cleanResponse = JsonSerializer.Deserialize<string>(responseContent);
                    messages.Add(MesageType.Agent, cleanResponse ?? string.Empty);
                }
                else
                {
                    messages.Add(MesageType.Agent, $"Error: Failed to get response from agent. Status: {response.StatusCode} Message: {response.RequestMessage}");
                }
            }
            catch (Exception ex)
            {
                messages.Add(MesageType.Agent, $"Error: {ex.Message}");
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