using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

internal class HttpDownloadClient : DownloadClient
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


    public HttpDownloadClient(GlobalBase clone, Dictionary<byte, int> rateLimitRequestsPerMinute) : base(clone, rateLimitRequestsPerMinute)
    {
        
    }
    
    protected override RequestResult MakeRequestInternal(string url, string? referrer = null)
    {
        HttpResponseMessage? response = null;
        while (response is null)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            if (referrer is not null)
                requestMessage.Headers.Referrer = new Uri(referrer);
            //Log($"Requesting {requestType} {url}");
            try
            {
                response = Client.Send(requestMessage);
            }
            catch (TaskCanceledException e)
            {
                Log($"Request timed out.\n\r{e}");
                return new RequestResult(HttpStatusCode.RequestTimeout, null, Stream.Null);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            Log($"Request-Error {response.StatusCode}: {response.ReasonPhrase}");
            return new RequestResult(response.StatusCode,  null, Stream.Null);
        }
        
        Stream stream = response.Content.ReadAsStream();

        HtmlDocument? document = null;

        if (response.Content.Headers.ContentType?.MediaType == "text/html")
        {
            StreamReader reader = new (stream);
            document = new ();
            document.LoadHtml(reader.ReadToEnd());
            stream.Position = 0;
        }

        // Request has been redirected to another page. For example, it redirects directly to the results when there is only 1 result
        if (response.RequestMessage is not null && response.RequestMessage.RequestUri is not null)
        {
            return new RequestResult(response.StatusCode, document, stream, true,
                response.RequestMessage.RequestUri.AbsoluteUri);
        }

        return new RequestResult(response.StatusCode, document, stream);
    }

    public override void Close()
    {
        Log("Closing.");
    }
}