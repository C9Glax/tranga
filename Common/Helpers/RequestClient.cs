using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.RateLimiting;

namespace Common.Helpers;

public sealed class RequestClient : HttpClient
{
    public RequestClient(RateLimiter limiter) : base(new ClientSideRateLimitedHandler(limiter))
    {
        DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Tranga", "2.1")));
    }
    
    public RequestClient()
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

    private sealed class ClientSideRateLimitedHandler(RateLimiter limiter)
        : DelegatingHandler(new HttpClientHandler()), IAsyncDisposable
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RateLimitLease? lease = null;
            while (lease is null) //Wait for lease
            {
                if (ct.IsCancellationRequested)
                    return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                if (await limiter.AcquireAsync(1, ct) is { IsAcquired: true } wow)
                {
                    lease = wow;
                }
                Thread.Sleep(10);
            }

            if (lease.IsAcquired)
                return await base.SendAsync(request, ct);

            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
            if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                response.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));

            return response;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await limiter.DisposeAsync().ConfigureAwait(false);

            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) limiter.Dispose();
        }
    }
}