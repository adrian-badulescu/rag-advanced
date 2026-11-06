using rag.Data;

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
        
        
        return services;
    }
}