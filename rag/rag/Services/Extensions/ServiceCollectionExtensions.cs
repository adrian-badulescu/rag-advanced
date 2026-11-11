using rag.Data;
using rag.Services.ChatServices;

namespace rag.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IHttpClientService, HttpClientService>();
        services.AddTransient<IEmbeddingService, EmbeddingService>();
        services.AddTransient<IDataIngestionService, DataIngestionService>();
        services.AddTransient<IDataRetrievalService, DataRetrievalService>();
        services.AddSingleton<IOnnxRerankerService, OnnxRerankerService>();
        services.AddScoped<IHybridSearchRepository,HybridSearchRepository>();
        services.AddSingleton<IChatHistoryManager, InMemoryChatHistoryManager>();
        
        return services;
    }
}