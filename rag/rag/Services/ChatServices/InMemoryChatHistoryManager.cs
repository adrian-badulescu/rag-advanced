using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using rag.Models;

namespace rag.Services.ChatServices;

public class InMemoryChatHistoryManager : IChatHistoryManager
{
    private readonly IMemoryCache _cache;
    private LLMConfig _options;
    
    public InMemoryChatHistoryManager(IMemoryCache cache, IOptions<LLMConfig> options)
    {
        _options = options.Value;
        _cache = cache;
        Console.WriteLine($"[DEBUG] InMemoryChatHistoryManager inițializat cu MaxHistoryMessages = {_options.MaxHistoryMessages}");
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

            if (history.Count > _options.MaxHistoryMessages)
            {
                history.RemoveRange(0, history.Count - _options.MaxHistoryMessages);
            }

            var sb = new StringBuilder();

            foreach (var msg in history)
            {
                sb.AppendLine($"{msg.Role}: {msg.Message}");
            }
            
            Console.WriteLine($"cache string - {sb};");
            return sb.ToString();
        }
    }
}