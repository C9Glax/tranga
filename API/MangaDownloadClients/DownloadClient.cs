using System.Net;
using log4net;

namespace API.MangaDownloadClients;

internal abstract class DownloadClient
{
    private readonly Dictionary<RequestType, DateTime> _lastExecutedRateLimit;
    protected ILog Log { get; init; }

    protected DownloadClient()
    {
        this.Log = LogManager.GetLogger(GetType());
        this._lastExecutedRateLimit = new();
    }
    
    public RequestResult MakeRequest(string url, RequestType requestType, string? referrer = null, string? clickButton = null)
    {
        Log.Debug($"Requesting {url}");
        if (!TrangaSettings.requestLimits.ContainsKey(requestType))
        {
            return new RequestResult(HttpStatusCode.NotAcceptable, null, Stream.Null);
        }

        int rateLimit = TrangaSettings.userAgent == TrangaSettings.DefaultUserAgent
            ? TrangaSettings.DefaultRequestLimits[requestType]
            : TrangaSettings.requestLimits[requestType];
        Log.Debug($"Request limit {rateLimit}");
        
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        _lastExecutedRateLimit.TryAdd(requestType, DateTime.UtcNow.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(DateTime.UtcNow.Subtract(_lastExecutedRateLimit[requestType]));

        
        if (rateLimitTimeout > TimeSpan.Zero)
        {
            Log.Debug($"Timeout: {rateLimitTimeout}");
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        _lastExecutedRateLimit[requestType] = DateTime.UtcNow;
        return result;
    }

    internal abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
}