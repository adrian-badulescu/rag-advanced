using rag.Data;
using rag.Models.Db;

namespace rag.Services;

public class DataRetrievalService : IDataRetrievalService
{
    private readonly RagDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IHybridSearchRepository _hybridSearchRepository;
    private readonly IOnnxRerankerService _rerankerService;

    public DataRetrievalService(RagDbContext dbContext, IEmbeddingService embeddingService, 
        IHybridSearchRepository hybridSearchRepository, IOnnxRerankerService  rerankerService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _hybridSearchRepository = hybridSearchRepository;
        _rerankerService = rerankerService;
    }
    
    public async Task<List<string>> GetRelevantContextAsync(string userQuery, string language = "english", int maxResults = 3)
    {
        
        float[] queryFloats = await _embeddingService.GetEmbeddingAsync(userQuery);
    
        List<TextChunk> candidates = await _hybridSearchRepository.GetHybridResultsAsync(queryFloats, userQuery, language, topK: 20);

        if (candidates == null || !candidates.Any()) return new();

        List<TextChunkWithScore> rankedChunks = _rerankerService.Rank(userQuery, candidates, 3);

        return rankedChunks
            .Take(maxResults)
            .Select(r => r.Chunk.Content)
            .ToList();
    }
}