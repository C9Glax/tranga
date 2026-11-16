using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Common.Settings;
using FlareSolverrSharp;

namespace Common.Helpers;

public sealed class RequestClient : HttpClient
{
    private static HttpMessageHandler CreateCloudflareHandler() => EnvVars.FlareSolverrUrl is null
        ? new HttpClientHandler()
        : new ClearanceHandler(EnvVars.FlareSolverrUrl)
        {
            MaxTimeout = 60000,
        };

    public RequestClient(RateLimiter limiter) : base(new ClientSideRateLimitedHandler(limiter, CreateCloudflareHandler()))
    {
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Tranga", "2.1")));
    }

    /// <summary>
    /// Creates a client on top of a caller-provided <paramref name="innerHandler"/> (e.g. one that refuses private
    /// addresses, or that talks to FlareSolverr only for some sources) instead of the global default handler.
    /// </summary>
    /// <param name="innerHandler">The handler requests are finally sent through</param>
    /// <param name="limiter">Optional client-side rate limiter placed in front of <paramref name="innerHandler"/></param>
    public RequestClient(HttpMessageHandler innerHandler, RateLimiter? limiter = null)
        : base(limiter is null ? innerHandler : new ClientSideRateLimitedHandler(limiter, innerHandler))
    {
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Tranga", "2.1")));
    }

    public RequestClient() : base(CreateCloudflareHandler())
    {
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Tranga", "2.1")));
    }
    
    public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return base.Send(request, cancellationToken);
    }

    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return base.SendAsync(request, cancellationToken);
    }

    public async Task<T?> SendAsyncAndParseJson<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await this.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}