namespace rag.Services.ChatServices;

public interface IChatHistoryManager
{
    string AddAndGetContext(string sessionId, string role, string message );
}