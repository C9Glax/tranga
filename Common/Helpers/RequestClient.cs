using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Common.Settings;
using FlareSolverrSharp;

namespace Common.Helpers;

public sealed class RequestClient : HttpClient
{
    private static readonly HttpMessageHandler CloudflareHandler = EnvVars.FlareSolverrUrl is null
        ? new HttpClientHandler()
        : new ClearanceHandler(EnvVars.FlareSolverrUrl)
        {
            MaxTimeout = 60000,
        };
    
    public RequestClient(RateLimiter limiter) : base(new ClientSideRateLimitedHandler(limiter, CloudflareHandler))
    {
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Tranga", "2.1")));
    }
    
    public RequestClient() : base(CloudflareHandler)
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