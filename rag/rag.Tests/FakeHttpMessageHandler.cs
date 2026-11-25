using System.Net;
using System.Net.Http.Json;

namespace rag.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    public HttpResponseMessage ResponseToReturn { get; set; } =  new HttpResponseMessage(HttpStatusCode.OK);
    public string LastRequestBody { get; private set; } = string.Empty;
    public HttpMethod? LastMethod { get; private set; }
    public Uri? LastRequestUri { get; private set; }

    public void SetJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        ResponseToReturn = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(payload)
        };
    }

    public void SetErrorResponse(HttpStatusCode statusCode, string errorMessage = "error")
    {
        ResponseToReturn = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorMessage)
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastMethod = request.Method;
        LastRequestUri = request.RequestUri;
        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return ResponseToReturn;
    }
}