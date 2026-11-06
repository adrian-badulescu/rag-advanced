using rag.Models.Db;

namespace rag.Data;

public interface IHybridSearchRepository
{
    Task<List<TextChunk>> GetHybridResultsAsync(float[] queryEmbedding,
        string queryText,
        string language,
        int topK = 20);
}