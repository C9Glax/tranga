using System.Net;
using System.Threading.RateLimiting;

namespace Common.Helpers;

internal sealed class ClientSideRateLimitedHandler(RateLimiter limiter, HttpMessageHandler? baseHandler = null)
    : DelegatingHandler(baseHandler ?? new HttpClientHandler()), IAsyncDisposable
{
    public static readonly TimeSpan DefaultAcquireLeaseTimeout = TimeSpan.FromSeconds(180);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(DefaultAcquireLeaseTimeout);

        RateLimitLease? lease = null;
        while (lease is null) //Wait for lease, or give up once acquireLeaseTimeout elapses
        {
            if (timeoutCts.IsCancellationRequested)
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);

            try
            {
                if (await limiter.AcquireAsync(1, timeoutCts.Token) is { IsAcquired: true } wow)
                    lease = wow;
            }
            catch (OperationCanceledException)
            {
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }

            Thread.Sleep(10);
        }

        return await base.SendAsync(request, ct);
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