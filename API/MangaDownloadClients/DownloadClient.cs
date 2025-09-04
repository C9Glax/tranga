using System.Collections.Concurrent;
using System.Net;
using log4net;

namespace API.MangaDownloadClients;

public abstract class DownloadClient
{
    private static readonly ConcurrentDictionary<RequestType, DateTime> LastExecutedRateLimit = new();
    protected ILog Log { get; init; }

    protected DownloadClient()
    {
        this.Log = LogManager.GetLogger(GetType());
    }
    
    // TODO Requests still go too fast across threads!
    public RequestResult MakeRequest(string url, RequestType requestType, string? referrer = null, string? clickButton = null)
    {
        Log.Debug($"Requesting {requestType} {url}");
        
        // If we don't have a RequestLimit set for a Type, use the default one
        if (!Tranga.Settings.RequestLimits.ContainsKey(requestType))
            requestType = RequestType.Default;

        int rateLimit = Tranga.Settings.RequestLimits[requestType];
        // TODO this probably needs a better check whether the useragent matches...
        // If the UserAgent is the default one, do not exceed the default request-limits.
        if (Tranga.Settings.UserAgent == TrangaSettings.DefaultUserAgent && rateLimit > TrangaSettings.DefaultRequestLimits[requestType])
            rateLimit = TrangaSettings.DefaultRequestLimits[requestType];
        
        // Apply the delay
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        DateTime now = DateTime.Now;
        LastExecutedRateLimit.TryAdd(requestType, now.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(now.Subtract(LastExecutedRateLimit[requestType]));
        Log.Debug($"Request limit {requestType} {rateLimit}/Minute timeBetweenRequests: {timeBetweenRequests:ss'.'fffff} Timeout: {rateLimitTimeout:ss'.'fffff}");
        
        if (rateLimitTimeout > TimeSpan.Zero)
            Thread.Sleep(rateLimitTimeout);

        // Make the request
        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        
        // Update the time the last request was made
        LastExecutedRateLimit[requestType] = DateTime.UtcNow;
        Log.Debug($"Result {url}: {result}");
        return result;
    }

    internal abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
}