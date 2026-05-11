using System.Threading.RateLimiting;
using Common.Helpers;

namespace Common.Tests.Helpers;

public sealed class ClientSideRateLimitedHandlerTests : RequestClientTests
{
    [Fact]
    public async Task RateLimitApplies()
    { 
        using TestServer server = new();
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
            HttpRequestMessage request = new(HttpMethod.Get, $"{TestServer.BaseUrl}?{DateTime.Now}");
            requests.Add(client.SendAsync(request, ct));
        }

        await Task.WhenAll(requests);
        DateTime end = DateTime.Now;
        
        Assert.True(end - start > TimeSpan.FromSeconds(1));
    }
}