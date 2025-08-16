using Agent2Agent.Web.Service;
using Agent2Agent.Web.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace Agent2Agent.Web.Pages;

public partial class Chat
{
	[Inject] IChatAgentService ChatAgentService { get; set; } = default!;
	[Inject] IChatHistoryService ChatHistory { get; set; } = default!;

	private IJSObjectReference _module = default!;

	private List<KeyValuePair<MessageType, string>> messages = new();
	private string threadId = Guid.NewGuid().ToString();
	private string currentMessage = string.Empty;
	private bool isProcessing;
	private bool isMessageAreaVisible;

	protected override async Task OnInitializedAsync()
	{
		_module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Pages/{nameof(Chat)}.razor.js")!;

		// Try to reuse existing thread id stored in sessionStorage (persists across browser refreshes)
		try
		{
			var existing = await _module.InvokeAsync<string>("getThreadId");
			if (!string.IsNullOrEmpty(existing))
			{
				threadId = existing;
			}
			else
			{
				await _module.InvokeVoidAsync("setThreadId", threadId);
			}
		}
		catch
		{
			// ignore JS interop/session storage failures
		}

		// Load in-memory history for this thread (exists while app is running)
		ChatHistory.EnsureThreadExists(threadId);
		var hist = ChatHistory.GetMessages(threadId);
		foreach (var entry in hist)
		{
			messages.Add(new(entry.IsUser ? MessageType.User : MessageType.Agent, entry.Content));
		}

		await base.OnInitializedAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			// Scroll to the latest message
			await _module.InvokeVoidAsync("ScrollToBottom", "thread-top");
		}
	}
	
	private void ToggleMessageArea()
	{
		isMessageAreaVisible = !isMessageAreaVisible;
	}

	private async Task HandleKeyUp(KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
		{
			await SendMessage();
		}
	}

	private async Task SendMessage()
	{
		if (!string.IsNullOrEmpty(currentMessage))
		{
			var messageToSend = currentMessage;
			messages.Add(new(MessageType.User, messageToSend));
			ChatHistory.AddMessage(threadId, new ChatMessageEntry(true, messageToSend));
			currentMessage = string.Empty;
			isProcessing = true;
			StateHasChanged();

			try
			{
				var response = await ChatAgentService.SendMessageAsync(threadId, messageToSend);
				messages.Add(new(MessageType.Agent, response));
				ChatHistory.AddMessage(threadId, new ChatMessageEntry(false, response));
			}
			catch (Exception ex)
			{
				var err = $"Error: {ex.Message}";
				messages.Add(new(MessageType.Agent, err));
				ChatHistory.AddMessage(threadId, new ChatMessageEntry(false, err));
			}
			finally
			{
				isProcessing = false;
				StateHasChanged();
			}
		}
	}
}