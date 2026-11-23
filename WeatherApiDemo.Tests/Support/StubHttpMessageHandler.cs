using System.Net;
using System.Text;

namespace WeatherApiDemo.Tests.Support;

internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
{
    public List<Uri> RequestedUris { get; } = [];

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestedUris.Add(request.RequestUri!);

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });
    }
}

internal sealed class StubHttpClientFactory(HttpMessageHandler handler, Uri baseAddress) : IHttpClientFactory
{
    public string? RequestedName { get; private set; }

    public HttpClient CreateClient(string name)
    {
        RequestedName = name;
        return new HttpClient(handler, disposeHandler: false) { BaseAddress = baseAddress };
    }
}
