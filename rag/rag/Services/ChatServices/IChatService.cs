namespace rag.Services.ChatServices;

public interface IChatService
{
    Task<string> GetAiResponseAsync(string session, string role, string prompt);
}