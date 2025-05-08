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
        
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        DateTime now = DateTime.Now;
        _lastExecutedRateLimit.TryAdd(requestType, now.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(now.Subtract(_lastExecutedRateLimit[requestType]));
        Log.Debug($"Request limit {rateLimit}/Minute timeBetweenRequests: {timeBetweenRequests:ss.fff} Timeout: {rateLimitTimeout:ss.fff}");
        
        if (rateLimitTimeout > TimeSpan.Zero)
        {
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        _lastExecutedRateLimit[requestType] = DateTime.UtcNow;
        return result;
    }

    internal abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
}