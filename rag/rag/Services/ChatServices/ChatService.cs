using Microsoft.Extensions.Options;
using rag.Models;

namespace rag.Services.ChatServices;

public class ChatService : IChatService
{
    private readonly IHttpClientService  _httpClientService;
    private readonly LLMConfig _options;
    private readonly EndpointsConfig _endpoints;
    private readonly IDataRetrievalService _retrievalService;
    private readonly IChatHistoryManager _historyManager;
    
    
    public ChatService(IHttpClientService  httpClientService, 
        IOptions<LLMConfig> options, 
        IOptions<EndpointsConfig> endpoints, 
        IDataRetrievalService retrievalService,
        IChatHistoryManager historyManager)
    {
        _httpClientService = httpClientService;
        _options = options.Value;
        _endpoints = endpoints.Value;
        _retrievalService = retrievalService;
        _historyManager = historyManager;
    }

    public async Task<string> GetAiResponseAsync(string session,string role,string prompt)
    {
        string chatHistoryContext = _historyManager.AddAndGetContext(session, role, prompt);
        Console.WriteLine($"chat service chatHistoryContext - {chatHistoryContext};");
        
        List<string> relevantChunks = await _retrievalService.GetRelevantContextAsync(prompt, maxResults: 3);
        string dbResult = string.Join("\n---\n", relevantChunks);
        
        string promptFinal = string.Format(_options.PromptTemplate, chatHistoryContext, dbResult, prompt);
        Console.WriteLine($"chat service promptFinal - {promptFinal};");
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptFinal }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = _options.MaxOutputTokens,
                temperature = _options.Temperature,
            }
        };
        
        try
        {
            var httpClient = _httpClientService.GetHttpClient("LLMClient");
            var response = await httpClient.PostAsJsonAsync(_endpoints.LLM.RelativePathChat, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Google API Error Details: {errorContent}");
                return $"Error in communication with Google Gemini API: {response.StatusCode} - {errorContent}";
            }
            
            var result =  await response.Content.ReadFromJsonAsync<GeminiChatResponse>();
            var responseText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                               ?? "No response returned from the LLM model.";
            
            var finalContext  = _historyManager.AddAndGetContext(session, "Assistant", responseText);
            Console.WriteLine($"chat service promptFinal - {finalContext};");
            return responseText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error: {ex.Message}");
            throw;
        }
    }
}