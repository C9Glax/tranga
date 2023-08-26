using System.Net;
using System.Net.Http.Headers;

namespace Tranga.MangaConnectors;

internal class DownloadClient : GlobalBase
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestHeaders =
            {
                UserAgent =
                {
                    new ProductInfoHeaderValue("Tranga", "0.1")
                }
            }
        };

        private readonly Dictionary<byte, DateTime> _lastExecutedRateLimit;
        private readonly Dictionary<byte, TimeSpan> _rateLimit;

        public DownloadClient(GlobalBase clone, Dictionary<byte, int> rateLimitRequestsPerMinute) : base(clone)
        {
            _lastExecutedRateLimit = new();
            _rateLimit = new();
            foreach(KeyValuePair<byte, int> limit in rateLimitRequestsPerMinute)
                _rateLimit.Add(limit.Key, TimeSpan.FromMinutes(1).Divide(limit.Value));
        }

        /// <summary>
        /// Request Webpage
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestType">For RateLimits: Same Endpoints use same type</param>
        /// <param name="referrer">Used in http request header</param>
        /// <returns>RequestResult with StatusCode and Stream of received data</returns>
        public RequestResult MakeRequest(string url, byte requestType, string? referrer = null)
        {
            if (_rateLimit.TryGetValue(requestType, out TimeSpan value))
                _lastExecutedRateLimit.TryAdd(requestType, DateTime.Now.Subtract(value));
            else
            {
                Log("RequestType not configured for rate-limit.");
                return new RequestResult(HttpStatusCode.NotAcceptable, Stream.Null);
            }

            TimeSpan rateLimitTimeout = _rateLimit[requestType]
                .Subtract(DateTime.Now.Subtract(_lastExecutedRateLimit[requestType]));
            
            if(rateLimitTimeout > TimeSpan.Zero)
                Thread.Sleep(rateLimitTimeout);

            HttpResponseMessage? response = null;
            while (response is null)
            {
                try
                {
                    HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
                    if(referrer is not null)
                        requestMessage.Headers.Referrer = new Uri(referrer);
                    _lastExecutedRateLimit[requestType] = DateTime.Now;
                    response = Client.Send(requestMessage);
                }
                catch (HttpRequestException e)
                {
                    Log("Exception:\n\t{0}\n\tWaiting {1} before retrying.", e.Message, _rateLimit[requestType] * 2);
                    Thread.Sleep(_rateLimit[requestType] * 2);
                }
            }
            if (!response.IsSuccessStatusCode)
            {
                Log($"Request-Error {response.StatusCode}: {response.ReasonPhrase}");
                return new RequestResult(response.StatusCode, Stream.Null);
            }

            // Request has been redirected to another page. For example, it redirects directly to the results when there is only 1 result
            if(response.RequestMessage is not null && response.RequestMessage.RequestUri is not null)
            {
                return new RequestResult(response.StatusCode, response.Content.ReadAsStream(), true, response.RequestMessage.RequestUri.AbsoluteUri);
            }

            return new RequestResult(response.StatusCode, response.Content.ReadAsStream());
        }

        public struct RequestResult
        {
            public HttpStatusCode statusCode { get; }
            public Stream result { get; }
            public bool hasBeenRedirected { get; }
            public string? redirectedToUrl { get; }

            public RequestResult(HttpStatusCode statusCode, Stream result)
            {
                this.statusCode = statusCode;
                this.result = result;
            }

            public RequestResult(HttpStatusCode statusCode, Stream result, bool hasBeenRedirected, string redirectedTo)
                : this(statusCode, result)
            {
                this.hasBeenRedirected = hasBeenRedirected;
                redirectedToUrl = redirectedTo;
            }
        }
    }