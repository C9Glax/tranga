using System.Net;
using HtmlAgilityPack;

namespace API.MangaDownloadClients;

internal class HttpDownloadClient : DownloadClient
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public HttpDownloadClient()
    {
        Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", TrangaSettings.userAgent);
        Client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    }
    
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        if (clickButton is not null)
            Log.Warn("Client can not click button");
        HttpResponseMessage? response = null;
        Uri uri = new(url);
        HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
        if (referrer is not null)
            requestMessage.Headers.Referrer = new (referrer);
        Log.Debug($"Requesting {url}");
        try
        {
            response = Client.Send(requestMessage);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new (HttpStatusCode.Unused, null, Stream.Null);
        }

        if (!response.IsSuccessStatusCode)
        {
            Log.Debug($"Request returned status code {response.StatusCode}:\n" +
                      $"\tRequest:\n" +
                      $"{requestMessage.Method} {requestMessage.RequestUri}\n" +
                      $"{requestMessage.Version} {requestMessage.VersionPolicy}\n" +
                      $"Headers:\n\t{string.Join("\n\t", requestMessage.Headers)}\n" +
                      $"{requestMessage.Content?.ReadAsStringAsync().Result}" +
                      $"\tResponse:\n" +
                      $"{response.Version}\n" +
                      $"Headers:\n\t{string.Join("\n\t", response.Headers)}\n" +
                      $"{response.Content.ReadAsStringAsync().Result}");
            return new (response.StatusCode,  null, Stream.Null);
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