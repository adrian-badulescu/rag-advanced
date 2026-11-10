using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using rag.Models;

namespace rag.Services;

public class InMemoryChatHistoryManager : IChatHistoryManager
{
    private readonly IMemoryCache _cache;
    private int _maxHistorySize;
    
    public InMemoryChatHistoryManager(IMemoryCache cache, IOptions<LLMConfig> options)
    {
        _maxHistorySize = options.Value.MaxHistoryMessages;
        _cache = cache;
    }

    public string AddAndGetContext(string sessionId, string role, string message)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "default-session";
        }
        
        var history = _cache.GetOrCreate(sessionId, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return new List<ChatRequest>();
        })!;

        lock (history)
        {
            history.Add(new ChatRequest { Role = role, Message = message });

            if (history.Count > _maxHistorySize)
            {
                history.RemoveRange(0, history.Count - _maxHistorySize);
            }

            var sb = new StringBuilder();

            foreach (var msg in history)
            {
                sb.AppendLine($"{msg.Role}: {msg.Message}");
            }
            
            return sb.ToString();
        }
    }
}