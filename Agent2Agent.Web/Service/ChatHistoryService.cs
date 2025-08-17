using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace Agent2Agent.Web.Service;

public record ChatMessageEntry(bool IsUser, string Content);

public interface IChatHistoryService
{
    List<ChatMessageEntry> GetMessages(string sessionId);
    void AddMessage(string sessionId, ChatMessageEntry message);
    void EnsureThreadExists(string sessionId);
    void ClearThread(string sessionId);
}

public class ChatHistoryService : IChatHistoryService
{
    private readonly ConcurrentDictionary<string, List<ChatMessageEntry>> _store = new();

    public List<ChatMessageEntry> GetMessages(string sessionId)
    {
        if (_store.TryGetValue(sessionId, out var list))
            return list.ToList();

        var newList = new List<ChatMessageEntry>();
        _store[sessionId] = newList;
        return newList;
    }

    public void AddMessage(string sessionId, ChatMessageEntry message)
    {
        var list = _store.GetOrAdd(sessionId, _ => new List<ChatMessageEntry>());
        lock (list)
        {
            list.Add(message);
        }
    }

    public void EnsureThreadExists(string sessionId)
    {
        _store.GetOrAdd(sessionId, _ => new List<ChatMessageEntry>());
    }

    public void ClearThread(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;

        _store.TryRemove(sessionId, out _);
    }
}