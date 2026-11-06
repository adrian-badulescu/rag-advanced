using rag.Models.Db;

namespace rag.Services;

public interface IOnnxRerankerService
{
    List<TextChunkWithScore> Rank(string query, List<TextChunk> documents, int topK);
}