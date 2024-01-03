using System.Net;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

internal abstract class DownloadClient : GlobalBase
{
    private readonly Dictionary<byte, DateTime> _lastExecutedRateLimit;
    private readonly Dictionary<byte, TimeSpan> _rateLimit;

    protected DownloadClient(GlobalBase clone,  Dictionary<byte, int> rateLimitRequestsPerMinute) : base(clone)
    {
        this._lastExecutedRateLimit = new();
        _rateLimit = new();
        foreach (KeyValuePair<byte, int> limit in rateLimitRequestsPerMinute)
            _rateLimit.Add(limit.Key, TimeSpan.FromMinutes(1).Divide(limit.Value));
    }
    
    public RequestResult MakeRequest(string url, byte requestType, string? referrer = null, string? clickButton = null)
    {
        if (_rateLimit.TryGetValue(requestType, out TimeSpan value))
            _lastExecutedRateLimit.TryAdd(requestType, DateTime.Now.Subtract(value));
        else
        {
            Log("RequestType not configured for rate-limit.");
            return new RequestResult(HttpStatusCode.NotAcceptable, null, Stream.Null);
        }

        TimeSpan rateLimitTimeout = _rateLimit[requestType]
            .Subtract(DateTime.Now.Subtract(_lastExecutedRateLimit[requestType]));

        if (rateLimitTimeout > TimeSpan.Zero)
        {
            Log($"Waiting {rateLimitTimeout.TotalSeconds} seconds");
            Thread.Sleep(rateLimitTimeout);
        }

        RequestResult result = MakeRequestInternal(url, referrer, clickButton);
        _lastExecutedRateLimit[requestType] = DateTime.Now;
        return result;
    }

    protected abstract RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null);
    public abstract void Close();
}