using System.Text.Json.Serialization;

namespace rag.Models;

public class IngestRequest
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";
    
    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;
    
    [JsonPropertyName("fullText")]
    public string FullText { get; set; } = string.Empty;
}