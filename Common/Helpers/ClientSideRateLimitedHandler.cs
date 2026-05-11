using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace Common.Helpers;

internal sealed class ClientSideRateLimitedHandler(RateLimiter limiter, HttpMessageHandler? baseHandler = null)
    : DelegatingHandler(baseHandler ?? new HttpClientHandler()), IAsyncDisposable
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
            response.Headers.Add("Retry-After",
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));

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