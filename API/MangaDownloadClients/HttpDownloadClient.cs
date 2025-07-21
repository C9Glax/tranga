using System.Net;
using HtmlAgilityPack;

namespace API.MangaDownloadClients;

internal class HttpDownloadClient : DownloadClient
{
    private static readonly FlareSolverrDownloadClient FlareSolverrDownloadClient = new();
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        if (clickButton is not null)
            Log.Warn("Client can not click button");
        HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        client.DefaultRequestHeaders.Add("User-Agent", Tranga.Settings.UserAgent);
        HttpResponseMessage? response;
        Uri uri = new(url);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
        if (referrer is not null)
            requestMessage.Headers.Referrer = new (referrer);
        Log.Debug($"Requesting {url}");
        try
        {
            response = client.Send(requestMessage);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new (HttpStatusCode.Unused, null, Stream.Null);
        }

        if (!response.IsSuccessStatusCode)
        {
            Log.Debug($"Request returned status code {(int)response.StatusCode} {response.StatusCode}");
            if (response.Headers.Server.Any(s =>
                    (s.Product?.Name ?? "").Contains("cloudflare", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Debug("Retrying with FlareSolverr!");
                return FlareSolverrDownloadClient.MakeRequestInternal(url, referrer, clickButton);
            }
            else
            {
                Log.Debug($"Request returned status code {(int)response.StatusCode} {response.StatusCode}:\n" +
                          $"=====\n" +
                          $"Request:\n" +
                          $"{requestMessage.Method} {requestMessage.RequestUri}\n" +
                          $"{requestMessage.Version} {requestMessage.VersionPolicy}\n" +
                          $"Headers:\n\t{string.Join("\n\t", requestMessage.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                          $"{requestMessage.Content?.ReadAsStringAsync().Result}" +
                          $"=====\n" +
                          $"Response:\n" +
                          $"{response.Version}\n" +
                          $"Headers:\n\t{string.Join("\n\t", response.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                          $"{response.Content.ReadAsStringAsync().Result}");
            }
        }

        Stream stream;
        try
        {
            stream = response.Content.ReadAsStream();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return new (HttpStatusCode.Unused, null, Stream.Null);
        }

        HtmlDocument? document = null;

        if (response.Content.Headers.ContentType?.MediaType == "text/html")
        {
            StreamReader reader = new (stream);
            document = new ();
            document.LoadHtml(reader.ReadToEnd());
            stream.Position = 0;
        }

        // Request has been redirected to another page. For example, it redirects directly to the results when there is only 1 result
        if (response.RequestMessage is not null && response.RequestMessage.RequestUri is not null && response.RequestMessage.RequestUri != uri)
        {
            return new (response.StatusCode, document, stream, true, response.RequestMessage.RequestUri.AbsoluteUri);
        }

        return new (response.StatusCode, document, stream);
    }
}