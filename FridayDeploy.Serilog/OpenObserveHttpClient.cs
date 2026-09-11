using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.Http;

namespace FridayDeploy.Serilog;

/// <summary>
/// Thin <see cref="IHttpClient"/> wrapper that adds the OpenObserve
/// <c>Authorization</c> header to every batch POST.
/// </summary>
internal sealed class OpenObserveHttpClient : IHttpClient
{
    private readonly HttpClient _client;

    public OpenObserveHttpClient(string token, TimeSpan timeout)
    {
        _client = new HttpClient { Timeout = timeout };
        // token is the full header value, e.g. "Basic <base64>"
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Configure(IConfiguration configuration) { }

    public async Task<HttpResponseMessage> PostAsync(
        string requestUri,
        Stream contentStream,
        CancellationToken cancellationToken)
    {
        using var content = new StreamContent(contentStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await _client.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _client.Dispose();
}
