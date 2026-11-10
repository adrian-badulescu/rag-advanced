namespace rag.Models;

public class LLMConfig
{
    public string RerankerModelPath { get; set; } = string.Empty;
    
    public int MaxHistoryMessages = 0;
    
    public string TokenizerPath  { get; set; } = string.Empty;
    public float SimilarityThreshold { get; set; } = 0.0f;
    public int MaxOutputTokens { get; set; } = 0;
    public float Temperature { get; set; } = 0;
    public int OutputDimensionality { get; set; } = 0;
    public string TaskType { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int GoogleChunkSize  { get; set; } = 0;
    public int IngestDocumentBatchSize  { get; set; } = 0;
    public int TextChunkSize { get; set; } = 0;
    public int TextOverlapSize   { get; set; } = 0;
    public string PromptTemplate { get; set; } = string.Empty;
}