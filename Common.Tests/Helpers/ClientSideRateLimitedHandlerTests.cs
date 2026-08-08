using System.Net;
using System.Threading.RateLimiting;
using Common.Helpers;

namespace Common.Tests.Helpers;

// ClientSideRateLimitedHandler is internal, so it's exercised only through the public
// RequestClient surface (no InternalsVisibleTo in this repo).
public sealed class ClientSideRateLimitedHandlerTests(TestServerFixture serverFixture): RequestClientTests(serverFixture)
{
    [Fact]
    public async Task RateLimitApplies()
    {
        RequestClient client = new(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            PermitLimit = 60,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1
        }));
        List<Task> requests = [];
        DateTime start = DateTime.Now;
        foreach (int _ in new int[120])
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{TestServerFixture.BaseUrl}?{DateTime.Now}");
            requests.Add(client.SendAsync(request, ct));
        }

        await Task.WhenAll(requests);
        DateTime end = DateTime.Now;

        Assert.True(end - start > TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AcquiresLeaseBeforeSendingEachRequest()
    {
        TestRateLimiter limiter = new(acquireSucceeds: true);
        RequestClient client = new(limiter);

        for (int i = 0; i < 3; i++)
        {
            HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);
            HttpResponseMessage response = await client.SendAsync(request, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal(3, limiter.AcquireAsyncCallCount);
    }

    [Fact]
    public async Task RetriesInsteadOfFailingFastWhenLeaseIsDenied()
    {
        // A permanently-denying limiter must not fail the request immediately with an error
        // status (e.g. 429) - it keeps retrying until the caller gives up.
        TestRateLimiter limiter = new(acquireSucceeds: false);
        RequestClient client = new(limiter);
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.SendAsync(request, cts.Token));

        Assert.True(limiter.AcquireAsyncCallCount > 1);
    }

    [Fact]
    public async Task ThrowsWhenCallerCancelsWhileWaitingForLease()
    {
        TestRateLimiter limiter = new(acquireSucceeds: false);
        RequestClient client = new(limiter);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        HttpRequestMessage request = new(HttpMethod.Get, TestServerFixture.BaseUrl);

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.SendAsync(request, cts.Token));
    }

    [Fact]
    public void DisposesLimiterOnDisposal()
    {
        TestRateLimiter limiter = new();
        RequestClient client = new(limiter);

        client.Dispose();

        Assert.True(limiter.Disposed);
    }
}