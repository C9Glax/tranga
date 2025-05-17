using System.Net;
using log4net;

namespace API.MangaDownloadClients;

internal abstract class DownloadClient
{
    private static readonly Dictionary<RequestType, DateTime> LastExecutedRateLimit = new();
    protected ILog Log { get; init; }

    protected DownloadClient()
    {
        this.Log = LogManager.GetLogger(GetType());
    }
    
    public RequestResult MakeRequest(string url, RequestType requestType, string? referrer = null, string? clickButton = null)
    {
        Log.Debug($"Requesting {requestType} {url}");
        if (!TrangaSettings.requestLimits.ContainsKey(requestType))
        {
            return new RequestResult(HttpStatusCode.NotAcceptable, null, Stream.Null);
        }

        int rateLimit = TrangaSettings.userAgent == TrangaSettings.DefaultUserAgent
            ? TrangaSettings.DefaultRequestLimits[requestType]
            : TrangaSettings.requestLimits[requestType];
        
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        DateTime now = DateTime.Now;
        LastExecutedRateLimit.TryAdd(requestType, now.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(now.Subtract(LastExecutedRateLimit[requestType]));
        Log.Debug($"Request limit {requestType} {rateLimit}/Minute timeBetweenRequests: {timeBetweenRequests:ss'.'fffff} Timeout: {rateLimitTimeout:ss'.'fffff}");
        
        if (rateLimitTimeout > TimeSpan.Zero)
        {
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        LastExecutedRateLimit[requestType] = DateTime.UtcNow;
        Log.Debug($"Result {url}: {result}");
        return result;
    }

    internal abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
}