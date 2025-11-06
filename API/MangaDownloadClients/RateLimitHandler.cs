using System.Net;
using System.Threading.RateLimiting;
using log4net;

namespace API.MangaDownloadClients;

public class RateLimitHandler() : DelegatingHandler(new HttpClientHandler())
{
    private ILog Log { get; init; } = LogManager.GetLogger(typeof(RateLimitHandler));

    private readonly RateLimiter _limiter = new TokenBucketRateLimiter(new()
    {
        AutoReplenishment = true,
        QueueLimit = 100,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        TokenLimit = Tranga.Settings.UserAgent.Equals(TrangaSettings.DefaultUserAgent) ? int.Min(Constants.RequestsPerMinute , 90) : Constants.RequestsPerMinute,
        TokensPerPeriod = Constants.RequestsPerMinute / 60
    });
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.DebugFormat("Requesting lease {0}", request.RequestUri);
        using RateLimitLease lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);
        Log.DebugFormat("Acquired lease {0}", request.RequestUri);

        return lease.IsAcquired
            ? await base.SendAsync(request, cancellationToken)
            : new (HttpStatusCode.TooManyRequests);
    }
}