using System.Text.Json.Serialization;

namespace rag.Models;

public record ChatRequest
{
    [JsonPropertyName("message")]
    public required string  Message { get; init; } 
    
    public string Role { get; set; } = String.Empty;
}