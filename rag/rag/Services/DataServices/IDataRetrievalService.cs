namespace rag.Services;

public interface IDataRetrievalService
{
    Task<List<string>> GetRelevantContextAsync(string userQuery, string language = "english", int maxResults = 3);
}