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
    
    public RequestResult MakeRequest(string url, byte requestType, string? referrer = null)
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

        RequestResult result = MakeRequestInternal(url, referrer);
        _lastExecutedRateLimit[requestType] = DateTime.Now;
        return result;
    }

    protected abstract RequestResult MakeRequestInternal(string url, string? referrer = null);
    public abstract void Close();
    
    public struct RequestResult
    {
        public HttpStatusCode statusCode { get; }
        public Stream result { get; }
        public bool hasBeenRedirected { get; }
        public string? redirectedToUrl { get; }
        public HtmlDocument? htmlDocument { get; }

        public RequestResult(HttpStatusCode statusCode, HtmlDocument? htmlDocument, Stream result)
        {
            this.statusCode = statusCode;
            this.htmlDocument = htmlDocument;
            this.result = result;
        }

        public RequestResult(HttpStatusCode statusCode, HtmlDocument? htmlDocument, Stream result, bool hasBeenRedirected, string redirectedTo)
            : this(statusCode, htmlDocument, result)
        {
            this.hasBeenRedirected = hasBeenRedirected;
            redirectedToUrl = redirectedTo;
        }
    }

}