using System.Net;
using log4net;

namespace API.MangaDownloadClients;

internal class HttpDownloadClient : IDownloadClient
{
    private static readonly HttpClient Client;
    private static readonly FlareSolverrDownloadClient FlareSolverrDownloadClient;
    private ILog Log { get; } = LogManager.GetLogger(typeof(HttpDownloadClient));

    static HttpDownloadClient()
    {
        var handler = (HttpMessageHandler?)Tranga.RateLimitHandler ?? new HttpClientHandler();

        Client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        // ✅ Fallback per User-Agent nullo o vuoto
        var ua = Tranga.Settings.UserAgent;
        if (string.IsNullOrWhiteSpace(ua))
            ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 15_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0 Safari/537.36";

        Client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
        Client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        Client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("it-IT,it;q=0.9,en-US;q=0.8,en;q=0.7");

        FlareSolverrDownloadClient = new(Client);
    }

    public async Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        Log.Debug($"Using {typeof(HttpDownloadClient).FullName} for {url}");
        HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
        if (referrer is not null)
            requestMessage.Headers.Referrer = new(referrer);
        Log.Debug($"Requesting {url}");

        try
        {
            HttpResponseMessage response = await Client.SendAsync(requestMessage, cancellationToken ?? CancellationToken.None);
            Log.Debug($"Request {url} returned {(int)response.StatusCode} {response.StatusCode}");
            if (response.IsSuccessStatusCode)
                return response;

            if (response.Headers.Server.Any(s =>
                    (s.Product?.Name ?? "").Contains("cloudflare", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Debug("Retrying with FlareSolverr!");
                return await FlareSolverrDownloadClient.MakeRequest(url, requestType, referrer);
            }

            Log.Debug($"Request returned status code {(int)response.StatusCode} {response.StatusCode}:\n" +
                      $"=====\n" +
                      $"Request:\n" +
                      $"{requestMessage.Method} {requestMessage.RequestUri}\n" +
                      $"{requestMessage.Version} {requestMessage.VersionPolicy}\n" +
                      $"Headers:\n\t{string.Join("\n\t", requestMessage.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}>"))}\n" +
                      $"{requestMessage.Content?.ReadAsStringAsync().Result}" +
                      $"=====\n" +
                      $"Response:\n" +
                      $"{response.Version}\n" +
                      $"Headers:\n\t{string.Join("\n\t", response.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}>"))}\n" +
                      $"{response.Content.ReadAsStringAsync().Result}");
            return new(HttpStatusCode.InternalServerError);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new(HttpStatusCode.InternalServerError);
        }
        catch (TaskCanceledException e)
        {
            Log.Error($"Timeout exceeded while requesting {url} → {e.Message}");
            return new(HttpStatusCode.RequestTimeout);
        }
    }
}
