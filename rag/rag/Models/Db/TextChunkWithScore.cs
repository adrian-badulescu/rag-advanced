namespace rag.Models.Db;

public class TextChunkWithScore
{
    public TextChunk Chunk { get; set; }
    public float Score { get; set; } 
}