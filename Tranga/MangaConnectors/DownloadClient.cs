using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Tranga.MangaConnectors;

internal abstract class DownloadClient : GlobalBase
{
    private readonly Dictionary<RequestType, DateTime> _lastExecutedRateLimit;

    protected DownloadClient(GlobalBase clone) : base(clone)
    {
        this._lastExecutedRateLimit = new();
    }
    
    public RequestResult MakeRequest(string url, RequestType requestType, string? referrer = null, string? clickButton = null)
    {
        if (!settings.requestLimits.ContainsKey(requestType))
        {
            logger?.LogError("RequestType not configured for rate-limit.");
            return new RequestResult(HttpStatusCode.NotAcceptable, null, Stream.Null);
        }

        int rateLimit = settings.userAgent == TrangaSettings.DefaultUserAgent
            ? TrangaSettings.DefaultRequestLimits[requestType]
            : settings.requestLimits[requestType];
        
        TimeSpan timeBetweenRequests = TimeSpan.FromMinutes(1).Divide(rateLimit);
        _lastExecutedRateLimit.TryAdd(requestType, DateTime.Now.Subtract(timeBetweenRequests));

        TimeSpan rateLimitTimeout = timeBetweenRequests.Subtract(DateTime.Now.Subtract(_lastExecutedRateLimit[requestType]));

        if (rateLimitTimeout > TimeSpan.Zero)
        {
            logger?.LogDebug($"Waiting {rateLimitTimeout.TotalSeconds} seconds");
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        _lastExecutedRateLimit[requestType] = DateTime.Now;
        return result;
    }

    protected abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
    public abstract void Close();
}