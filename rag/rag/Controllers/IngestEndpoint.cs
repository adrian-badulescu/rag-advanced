using Microsoft.AspNetCore.Mvc;
using rag.Models;
using rag.Services;

namespace rag.Controllers;

public static class IngestEndpoint
{
    public static IEndpointRouteBuilder MapIngestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ingest",
            async ([FromBody] IngestRequest request, [FromServices] IDataIngestionService ingestionService) =>
            {
                Console.WriteLine("--- A AJUNS CEREREA PE /api/ingest ---");
                if (string.IsNullOrWhiteSpace(request.DocumentName) || string.IsNullOrWhiteSpace(request.FullText))
                {
                    return Results.BadRequest("Document name and full text are required.");
                }

                try
                {
                    await ingestionService.IngestDocumentAsync(request.DocumentName, request.FullText, "en");
                    return Results.Ok(new { message = $"Document '{request.DocumentName}' successfully ingested and vectorized." });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Ingestion failed: {ex.Message}");
                }
            });

        return endpoints;

    }
}