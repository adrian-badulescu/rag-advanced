using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;
using rag.Models.Db;

namespace rag.Data;

public class HybridSearchRepository : IHybridSearchRepository
{
    private readonly RagDbContext _dbContext;

    public HybridSearchRepository(RagDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<List<TextChunk>> GetHybridResultsAsync(float[] queryEmbedding,
        string queryText, 
        string language,
        int topK = 20)
    {
        var allowedLanguages = new[] { "english", "romanian" };
        if (!allowedLanguages.Contains(language.ToLower()))
        {
            language = "english"; 
        }
        
        var sql = $@"
            WITH vector_search AS (
                SELECT ""Key"", ROW_NUMBER() OVER (ORDER BY ""Embedding"" <=> @vector) as rank
                FROM ""TextChunks""
                ORDER BY ""Embedding"" <=> @vector
                LIMIT @topK
            ),
            text_search AS (
                SELECT ""Key"", ROW_NUMBER() OVER (ORDER BY ts_rank_cd(to_tsvector('{language}', ""Content""), plainto_tsquery('{language}', @query)) DESC) as rank
                FROM ""TextChunks""
                WHERE to_tsvector('{language}', ""Content"") @@ plainto_tsquery('{language}', @query)
                ORDER BY ts_rank_cd(to_tsvector('{language}', ""Content""), plainto_tsquery('{language}', @query)) DESC
                LIMIT @topK
            )
            SELECT 
                c.""Key"", c.""Content"", c.""Document"", c.""Embedding""
            FROM ""TextChunks"" c
            LEFT JOIN vector_search v ON c.""Key"" = v.""Key""
            LEFT JOIN text_search t ON c.""Key"" = t.""Key""
            WHERE v.""Key"" IS NOT NULL OR t.""Key"" IS NOT NULL
            ORDER BY (COALESCE(1.0 / (60.0 + v.rank), 0.0) + COALESCE(1.0 / (60.0 + t.rank), 0.0)) DESC
            LIMIT @topK;";

        
        var parameters = new[]
        {
            new NpgsqlParameter("@vector", new Vector(queryEmbedding)),
            new NpgsqlParameter("@query", queryText),
            new NpgsqlParameter("@topK", topK)
        };
        
        return await _dbContext.TextChunks
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .ToListAsync();
    }
}