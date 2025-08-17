using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent2Agent.AgentA.Services;


record ConversationThread(string Id, ChatHistory History);

public interface IConversationService
{
    Task<string> SendMessageAsync(ChatMessageContent messageContent, ChatCompletionAgent agent, CancellationToken cancellation = default);
    Task<bool> ClearThreadAsync(string authorName, CancellationToken cancellation = default);
}

internal class ConversationService : IConversationService
{
    private readonly ILogger<ConversationService> _logger;
    private readonly IDistributedCache _cache;

    public ConversationService(ILogger<ConversationService> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<string> SendMessageAsync(ChatMessageContent messageContent, ChatCompletionAgent agent, CancellationToken cancellation = default)
    {
        var response = new StringBuilder();

        try
        {
            var thread = await _GetAgentThread(messageContent.AuthorName!, cancellation);
            thread.ChatHistory.Add(messageContent);

            await foreach (var result in agent.InvokeAsync(messageContent, thread, cancellationToken: cancellation))
            {
                response.Append(result.Message.Content);
            }

            await _SaveAgentThread(thread, cancellation);
        }
        catch (Exception ex)
        {
			_logger.LogError(ex, "Task failed with unexpected error.");
        }

        return response.ToString();
    }

    private async Task<ChatHistoryAgentThread> _GetAgentThread(string authorName, CancellationToken cancellation)
    {
        var threadData = await _cache.GetStringAsync($"thread_{authorName}", cancellation);
        if (string.IsNullOrEmpty(threadData))
        {
            var newThread = new ChatHistoryAgentThread(new ChatHistory(), authorName);
            await _SaveAgentThread(newThread, cancellation);
            return newThread;
        }
        else
        {
            var thread = JsonSerializer.Deserialize<ConversationThread>(threadData);
            if (thread == null)
            {
                throw new InvalidOperationException("Failed to deserialize conversation thread.");
            }

            return new ChatHistoryAgentThread(thread.History, thread.Id);
        }
    }

    private async Task _SaveAgentThread(ChatHistoryAgentThread thread, CancellationToken cancellation)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        var json = JsonSerializer.Serialize(new ConversationThread(thread.Id!, thread.ChatHistory));
        await _cache.SetStringAsync($"thread_{thread.Id}", json, options, cancellation);
    }

    public async Task<bool> ClearThreadAsync(string authorName, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            _logger.LogWarning("Author name is null or empty. Cannot clear thread.");
            return false;
        }

        try
        {
            var key = $"thread_{authorName}";
            var existing = await _cache.GetStringAsync(key, cancellation);
            if (string.IsNullOrEmpty(existing))
            {
                _logger.LogInformation("No conversation thread found for {AuthorName}", authorName);
                return false;
            }

            await _cache.RemoveAsync(key, cancellation);
            _logger.LogInformation("Cleared conversation thread for {AuthorName}", authorName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear conversation thread for {AuthorName}", authorName);
            throw;
        }
    }
}
