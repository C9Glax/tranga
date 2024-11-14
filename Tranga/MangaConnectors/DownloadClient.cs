using System.Net;
using log4net;
using log4net.Config;

namespace Tranga.MangaConnectors;

public abstract class DownloadClient
{
    private readonly Dictionary<RequestType, DateTime> _lastExecutedRateLimit;
    protected readonly ILog log;

    protected DownloadClient()
    {
        log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
        this._lastExecutedRateLimit = new();
    }
    
    public RequestResult MakeRequest(string url, RequestType requestType, string? referrer = null, string? clickButton = null)
    {
        if (!TrangaSettings.requestLimits.ContainsKey(requestType))
        {
            log.Info("RequestType not configured for rate-limit.");
            return new RequestResult(HttpStatusCode.NotAcceptable, null, Stream.Null);
        }

        int rateLimit = TrangaSettings.userAgent == TrangaSettings.DefaultUserAgent
            ? TrangaSettings.DefaultRequestLimits[requestType]
            : TrangaSettings.requestLimits[requestType];
        
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        _lastExecutedRateLimit.TryAdd(requestType, DateTime.Now.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(DateTime.Now.Subtract(_lastExecutedRateLimit[requestType]));

        if (rateLimitTimeout > TimeSpan.Zero)
        {
            log.Info($"Waiting {rateLimitTimeout.TotalSeconds} seconds");
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        _lastExecutedRateLimit[requestType] = DateTime.Now;
        return result;
    }

    internal abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
}