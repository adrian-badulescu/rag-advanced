using Microsoft.AspNetCore.Mvc;
using rag.Models;
using rag.Services.ChatServices;

namespace rag.Controllers;

public static class ChatsEndpoint
{
    public static IEndpointRouteBuilder MapChatEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", async ([FromBody]ChatRequest request, IChatService chatService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1000)
            {
                return Results.BadRequest(new {error = "Please provide a message or a shorter one"});
            }
            
            Console.WriteLine($"ENDPOINT - {request.SessionId} - {request.Role} - {request.Message}");
            
            string responseMessage = await chatService.GetAiResponseAsync(request.SessionId, request.Role,request.Message);

            return Results.Ok(responseMessage);
        }).RequireRateLimiting("chat-policy");

        return app;
    }
}