using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

internal class HttpDownloadClient : DownloadClient
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };


    public HttpDownloadClient(GlobalBase clone, Dictionary<byte, int> rateLimitRequestsPerMinute) : base(clone, rateLimitRequestsPerMinute)
    {
        if (settings.customUserAgent is null || settings.customUserAgent.Length < 1)
            Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Tranga", "1.0"));
        else
            Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", settings.customUserAgent);
    }
    
    protected override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        if(clickButton is not null)
            Log("Can not click button on static site.");
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
            catch (Exception e)
            {
                switch (e)
                {
                    case TaskCanceledException:
                        Log($"Request timed out {url}.\n\r{e}");
                        return new RequestResult(HttpStatusCode.RequestTimeout, null, Stream.Null);
                    case HttpRequestException:
                        Log($"Request failed {url}\n\r{e}");
                        return new RequestResult(HttpStatusCode.BadRequest, null, Stream.Null);
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            Log($"Request-Error {response.StatusCode}: {url}");
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