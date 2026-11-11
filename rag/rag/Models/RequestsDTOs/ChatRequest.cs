using System.Text.Json.Serialization;

namespace rag.Models;

public record ChatRequest
{
    
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; }
    
    [JsonPropertyName("message")]
    public required string  Message { get; init; } 
    
    public string Role { get; set; } = "User";
}