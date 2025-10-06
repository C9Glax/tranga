using System.Net;
using System.Threading.RateLimiting;
using log4net;

namespace API.MangaDownloadClients;

public class RateLimitHandler() : DelegatingHandler(new HttpClientHandler())
{
    private ILog Log { get; init; } = LogManager.GetLogger(typeof(RateLimitHandler));

    private readonly RateLimiter _limiter = new SlidingWindowRateLimiter(new ()
    {
        AutoReplenishment = true,
        PermitLimit = 240,
        QueueLimit = 120,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        SegmentsPerWindow = 60,
        Window = TimeSpan.FromSeconds(60)
    });
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.Debug($"Requesting lease {request.RequestUri}");
        using RateLimitLease lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);
        Log.Debug($"lease {lease.IsAcquired} {request.RequestUri}");

        return lease.IsAcquired
            ? await base.SendAsync(request, cancellationToken)
            : new (HttpStatusCode.TooManyRequests);
    }
}