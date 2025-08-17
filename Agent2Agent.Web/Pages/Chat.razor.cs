using Agent2Agent.Web.Service;
using Agent2Agent.Web.Shared;

using BlazorBootstrap;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace Agent2Agent.Web.Pages;

public partial class Chat
{
	[Inject] IChatAgentService _chatAgentService { get; set; } = default!;
	[Inject] IChatHistoryService _chatHistory { get; set; } = default!;
    [Inject] ILogger<Chat> _logger { get; set; } = default!;

    private Button _newConversationButton = default!;
	private IJSObjectReference _module = default!;
	private List<KeyValuePair<MessageType, string>> _messages = new();
	private string _threadId = Guid.NewGuid().ToString();
	private string _currentMessage = string.Empty;
	private bool _isProcessing;
	private bool _isMessageAreaVisible;

	protected override async Task OnInitializedAsync()
	{
		_module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Pages/{nameof(Chat)}.razor.js")!;

		// Try to reuse existing thread id stored in sessionStorage (persists across browser refreshes)
        try
        {
            var existing = await _module.InvokeAsync<string>("getThreadId");
            if (!string.IsNullOrEmpty(existing))
            {
                _threadId = existing;
            }
            else
            {
                await _module.InvokeVoidAsync("setThreadId", _threadId);
            }
        }
        catch
        {
            // ignore JS interop/session storage failures
        }

		// Load in-memory history for this thread (exists while app is running)
		_chatHistory.EnsureThreadExists(_threadId);
		var hist = _chatHistory.GetMessages(_threadId);
		foreach (var entry in hist)
		{
			_messages.Add(new(entry.IsUser ? MessageType.User : MessageType.Agent, entry.Content));
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
		_isMessageAreaVisible = !_isMessageAreaVisible;
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
		if (!string.IsNullOrEmpty(_currentMessage))
		{
			var messageToSend = _currentMessage;
			_messages.Add(new(MessageType.User, messageToSend));
			_chatHistory.AddMessage(_threadId, new ChatMessageEntry(true, messageToSend));
			_currentMessage = string.Empty;
			_isProcessing = true;
			StateHasChanged();

			try
			{
				var response = await _chatAgentService.SendMessageAsync(_threadId, messageToSend);
				_messages.Add(new(MessageType.Agent, response));
				_chatHistory.AddMessage(_threadId, new ChatMessageEntry(false, response));
			}
			catch (Exception ex)
			{
				var err = $"Error: {ex.Message}";
				_messages.Add(new(MessageType.Agent, err));
				_chatHistory.AddMessage(_threadId, new ChatMessageEntry(false, err));
			}
			finally
			{
				_isProcessing = false;
				StateHasChanged();
			}
		}
	}

    private async Task StartNewConversation()
    {
        _newConversationButton.ShowLoading();
        StateHasChanged();

        try
        {
            // If there are no interactions, do nothing
            var current = _chatHistory.GetMessages(_threadId);
            if (current == null || current.Count == 0)
                return;

            var oldThreadId = _threadId;
            await _chatAgentService.ClearSessionAsync(oldThreadId);

            // Optimistically clear client-side UI immediately
            _chatHistory.ClearThread(oldThreadId);
            _messages.Clear();

            // Create and persist new thread id locally
            _threadId = Guid.NewGuid().ToString();
            await _module.InvokeVoidAsync("setThreadId", _threadId);

            _chatHistory.EnsureThreadExists(_threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new conversation");
            // Log locally via a UI message
            _messages.Add(new(MessageType.Agent, $"Failed to start new conversation: {ex.Message}"));
        }
        finally
        {
            _newConversationButton.HideLoading();
            StateHasChanged();
        }
	}
}
