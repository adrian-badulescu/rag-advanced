namespace rag.Services;

public interface IChatHistoryManager
{
    string AddAndGetContext(string sessionId, string role, string message );
}