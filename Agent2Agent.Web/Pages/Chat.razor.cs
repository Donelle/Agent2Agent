using Agent2Agent.Web.Service;

using Markdig;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Agent2Agent.Web.Pages;

public partial class Chat
{
	enum MesageType
	{
		User,
		Agent
	}

	[Inject] IChatAgentService ChatAgentService { get; set; } = default!;

	private IJSObjectReference? _module;
	private List<KeyValuePair<MesageType, string>> messages = new();
	private string currentMessage = string.Empty;
	private bool isProcessing;

	protected override async Task OnInitializedAsync()
	{
		_module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Pages/{nameof(Chat)}.razor.js");
		await base.OnInitializedAsync();
	}

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
			isProcessing = true; 
			StateHasChanged();

			// Scroll to the latest message
			await _module!.InvokeVoidAsync("ScrollToBottom", "thinkingindicator");

			try
			{
				var response = await ChatAgentService.SendMessageAsync(messageToSend);	
				messages.Add(new(MesageType.Agent, response));
			}
			catch (Exception ex)
			{
				messages.Add(new(MesageType.Agent, $"Error: {ex.Message}"));
			}
			finally
			{
				isProcessing = false; 
				StateHasChanged();
			}
		}
	}

	public RenderFragment MarkdownFragment(string input)
	{
		var pipeline = new MarkdownPipelineBuilder().DisableHtml().UseAdvancedExtensions().Build();
		return (RenderTreeBuilder b) =>
		{
			Markdig.Blazor.Markdown.RenderToFragment(input, b, pipeline);
		};
	}
}