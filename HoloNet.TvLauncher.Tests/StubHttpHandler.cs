using System.Net;
using System.Net.Http;
using System.Text;

namespace HoloNet.TvLauncher.Tests;

/// <summary>Answers every request with the same JSON response and remembers the last URL asked for.</summary>
internal sealed class StubHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public string? RequestedUrl { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrl = request.RequestUri?.ToString();
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
