using System.Text;
using DefaultNamespace;
using Microsoft.Extensions.Options;
using Pgvector;
using rag.Data;
using rag.Models;
using rag.Models.Db;

using Catalyst;
using Catalyst.Models;
using Mosaik.Core;

namespace rag.Services;

public class DataIngestionService : IDataIngestionService
{
    private readonly RagDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly LLMConfig _llmOptions;

    public DataIngestionService(RagDbContext dbContext,
        IEmbeddingService embeddingService, IOptions<LLMConfig> llmOptions)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _llmOptions = llmOptions.Value;
    }

    public async Task<List<string>> CreateSemanticChunksAsync(List<string> sentences)
    {
        var chunks = new List<string>();
        if (sentences == null || sentences.Count == 0) return chunks;

        // 1: Generate embeddings for all sentences
        var embeddingsList = await _embeddingService.GetEmbeddingsAsync(sentences.ToArray());
        var embeddings = embeddingsList.ToList();

        // 2: loop for grouping based on consecutive similarity
        var currentChunkBuilder = new StringBuilder();
        currentChunkBuilder.Append(sentences[0]); // Start with first position

        for (int i = 0; i < sentences.Count - 1; i++)
        {
            // Measure the similarity of current and the next one
            float similarity = MathUtils.GetCosineSimilarity(embeddings[i], embeddings[i + 1]);

            if (similarity >= _llmOptions.SimilarityThreshold)
            {
                // If are similar(over threshold) we add the frase into the same chunk
                currentChunkBuilder.Append(" " + sentences[i + 1]);
            }
            else
            {
                // if the idea is under the threshold, close chunk and start a new one
                chunks.Add(currentChunkBuilder.ToString());
                currentChunkBuilder.Clear();
                currentChunkBuilder.Append(sentences[i + 1]);
            }

        }
        // adding the last chunk in the string builder
        if (currentChunkBuilder.Length > 0)
        {
            chunks.Add(currentChunkBuilder.ToString());
        }

        return chunks;
    }
    
    public async Task IngestDocumentAsync(string documentName, string rawText, string languageCode = "en")
    {
        try
        {
            Catalyst.Models.English.Register();
            Catalyst.Models.Romanian.Register();
            
            // Step A: Initialize Catalyst
            Storage.Current = new DiskStorage(AppDomain.CurrentDomain.BaseDirectory);
        
            var lang = languageCode == "en" ? Language.English : Language.Romanian;
            var pipeline = await Pipeline.ForAsync(lang);

            // Step B: Process text and transform it into IDocument obj
            var doc = new Document(rawText, lang);
            var processedDocs = pipeline.Process(new[] { doc });
            var evaluationResult = processedDocs.ToList();

            // Step C: Extract sentences
            var firstProcessedDoc = evaluationResult.FirstOrDefault();
            if (firstProcessedDoc == null) return;

            // Step D: Making semantic chunks
            List<string> sentences = firstProcessedDoc
                .Select(sentence => sentence.TokenizedValue)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (!sentences.Any()) return;
            
            List<string> semanticChunks = await CreateSemanticChunksAsync(sentences);

            // Step E: Generate the vector and save it in Db
            foreach (var chunkContent in semanticChunks)
            {
                if (string.IsNullOrWhiteSpace(chunkContent)) continue;
            
                float[] finalEmbeddingVector = await _embeddingService.GetEmbeddingAsync(chunkContent);
            
                var textChunk = new TextChunk
                {
                    Content = chunkContent,
                    Document = documentName,
                    Embedding = new Vector(finalEmbeddingVector)
                };

                _dbContext.TextChunks.Add(textChunk);
            }
        
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in semantic ingestion: {ex.Message}");
            throw;
        }

    }

    public async Task IngestDocumentAsync(string documentName, string fullText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullText))
            {
                return;
            }

            int chunkSize = _llmOptions.TextChunkSize;
            int overlap = _llmOptions.TextOverlapSize;
            
            var chunks = SplitIntoChunks(fullText, chunkSize, overlap)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
            
            if (!chunks.Any()) return;
            // groups database transaction number
            int batchSize = _llmOptions.IngestDocumentBatchSize;
            
            var chunkBatches = chunks.Chunk(batchSize);
            
            foreach (var batch in chunkBatches)
            {
                var batchEmbeddings = await _embeddingService.GetEmbeddingsAsync(batch);
                
                for (int i = 0; i < batch.Length; i++)
                {
                    var textChunk = new TextChunk
                    {
                        Content = batch[i],
                        Document = documentName,
                        Embedding = new Vector(batchEmbeddings[i])  
                    };

                    _dbContext.TextChunks.Add(textChunk);
                }
                await _dbContext.SaveChangesAsync();
                _dbContext.ChangeTracker.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    
    private List<string> SplitIntoChunks(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (text.Length <= chunkSize)
        {
            chunks.Add(text);
            return chunks;
        }

        int i = 0;
        while (i < text.Length)
        {
            if (i + chunkSize >= text.Length)
            {
                chunks.Add(text.Substring(i));
                break;
            }

            chunks.Add(text.Substring(i, chunkSize));
            i += (chunkSize - overlap); 
        }

        return chunks;
    }
}