namespace rag.Services;

public interface IDataRetrievalService
{
    // Task<List<string>> GetRelevantContextAsync(string userQuery, int maxResults = 3);
    Task<List<string>> GetRelevantContextAsync(string userQuery, string language = "english", int maxResults = 3);
}