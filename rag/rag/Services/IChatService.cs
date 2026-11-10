namespace rag.Services;

public interface IChatService
{
    Task<string> GetAiResponseAsync(string prompt);
    // Task<string> GetConversationalContextAsync(string sentences);
}