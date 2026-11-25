using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Moq;
using rag.Models;
using rag.Services;
using rag.Services.ChatServices;

namespace rag.Tests.Services;


[TestClass]
public class ChatServiceTests
{
    private Mock<IChatHistoryManager> _mockHistoryManager = null!;
    private Mock<IDataRetrievalService> _mockDataRetrievalService = null!;
    private FakeHttpMessageHandler _fakeHttpHandler = null!;
    private IOptions<LLMConfig> _llmOptions;
    private IOptions<EndpointsConfig> _endpointsOptions;
    private Mock<IHttpClientService> _mockHttpClientService = null!;
    private ChatService _sut = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _llmOptions = Options.Create(new LLMConfig
        {
            MaxOutputTokens = 2500,
            Temperature = 0.1f,
            PromptTemplate = "You are an AI utility for a Restaurant POS system. Use only the information from the section 'Context (Database Documentation)' AND the section 'Conversation context (history)' in order to respond in the language of the question made by the client of the POS system. If you do not find the answer into these sections, just say politely in the language of the question that you don't have this information.\\\\n\\\\n### Conversation context (history):\\\\n{0}\\\\n\\\\n### Context (Database Documentation):\\\\n{1}\\\\n\\\\n### Client question:\\\\n{2}\\\\n\\\\n### Answer:"
        });
        
        _endpointsOptions = Options.Create(new EndpointsConfig()
        {
            LLM = new LLMUrlConfig
            {
                RelativePathChat = "v1beta/models/gemini-3.5-flash-lite:generateContent"
            }
        });
        
        _fakeHttpHandler = new FakeHttpMessageHandler();
        
        var httpClient = new HttpClient(_fakeHttpHandler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
        
        _mockHttpClientService =  new Mock<IHttpClientService>();
        _mockHttpClientService
            .Setup(x => x.GetHttpClient("LLMClient"))
            .Returns(httpClient);
        
        _mockHistoryManager = new Mock<IChatHistoryManager>();
        _mockDataRetrievalService = new Mock<IDataRetrievalService>();
        _sut = new ChatService(
            _mockHttpClientService.Object,
            _llmOptions,
            _endpointsOptions,
            _mockDataRetrievalService.Object,
            _mockHistoryManager.Object
            );
    }

    [TestMethod]
    public async Task GetAiResponseAsync_GetHistoryContext_InitialEmptyContext_PassesContextToPrompt()
    {
        // 1. ARRANGE
        string session = "session-empty";
        string role = "User";
        string prompt = "How does a reseller create a restaurant?";
        string expectedHistory = "User: How does a reseller create a restaurant?\n";
        
        _mockHistoryManager
            .Setup(x => x.AddAndGetContext(session, role, prompt))
            .Returns(expectedHistory);
        
        _mockDataRetrievalService
            .Setup(x => x.GetRelevantContextAsync(prompt, "english", 3))
            .ReturnsAsync(new List<string> { "Doc chunk" });

        _fakeHttpHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new GeminiChatResponse
            {
                Candidates = new List<Candidate>
                {
                    new Candidate
                    {
                        Content = new GeminiContent
                        {
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "OK" } }
                        }
                    }
                }
            })
        };
        
        // 2. ACT
        await _sut.GetAiResponseAsync(session, role, prompt);
        
        // 3. ASSERT
        _mockHistoryManager.Verify(
            x => x.AddAndGetContext(session, role, prompt),
            Times.Once
        );

        
        Assert.IsNotNull(_fakeHttpHandler.LastRequestBody);
        Assert.IsTrue(_fakeHttpHandler.LastRequestBody.Contains("User: How does a reseller create a restaurant?"));
    }
    
    [TestMethod]
    public async Task GetAiResponseAsync_GetHistoryContext_PopulatedContext()
    {
        // 1. ARRANGE
        string session = "session-empty";
        string role = "User";
        string prompt = "How does a reseller create a restaurant?";
        string expectedHistory = "User: How does a reseller create a restaurant?\n" +
                                 "model: You go to the reseller dashboard and click Add Restaurant.\n" +
                                 "User: What about after the restaurant is created?\n";
        
        _mockHistoryManager
            .Setup(x => x.AddAndGetContext(session, role, prompt))
            .Returns(expectedHistory);
        
        _mockDataRetrievalService
            .Setup(x => x.GetRelevantContextAsync(prompt, "english", 3))
            .ReturnsAsync(new List<string> { "Post-creation setup doc chunk" });
        
        
        _fakeHttpHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new GeminiChatResponse
            {
                Candidates = new List<Candidate>
                {
                    new Candidate
                    {
                        Content = new GeminiContent
                        {
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "Configure menu and tables."} }
                        }
                    }
                }
            })
        };
        
        // 2. ACT
        await _sut.GetAiResponseAsync(session, role, prompt);
        
        // 3. ASSERT
        _mockHistoryManager.Verify(
            x => x.AddAndGetContext(session, role, prompt),
            Times.Once
        );

        Assert.IsNotNull(_fakeHttpHandler.LastRequestBody);
        Assert.IsTrue(_fakeHttpHandler.LastRequestBody.Contains("User: How does a reseller create a restaurant?"));
        Assert.IsTrue(_fakeHttpHandler.LastRequestBody.Contains("model: You go to the reseller dashboard and click Add Restaurant."));
        Assert.IsTrue(_fakeHttpHandler.LastRequestBody.Contains("User: What about after the restaurant is created?"));
        
    }

    [TestMethod]
    public async Task GetAiResponseAsync_GetHistoryContext_ReturnError()
    {
        // 1. ARRANGE
        string session = "session-error-500";
        string role = "User";
        string prompt = "How does a reseller create a restaurant?";
        
        _mockHistoryManager
            .Setup(x => x.AddAndGetContext(session, role, prompt))
            .Returns($"User: {prompt}\n");

        _mockDataRetrievalService
            .Setup(x => x.GetRelevantContextAsync(prompt, "english", 3))
            .ReturnsAsync(new List<string> { "Doc chunk" });
        
        _fakeHttpHandler.SetErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        // 2. ACT & 3. ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await _sut.GetAiResponseAsync(session, role, prompt);
        });
    }
}