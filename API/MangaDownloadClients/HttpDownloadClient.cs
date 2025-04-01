using System.Net;
using API.Schema;
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
    }
    
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        //TODO
        //if (clickButton is not null)
            //Log("Can not click button on static site.");
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
                        return new RequestResult(HttpStatusCode.RequestTimeout, null, Stream.Null);
                    case HttpRequestException:
                        return new RequestResult(HttpStatusCode.BadRequest, null, Stream.Null);
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            return new RequestResult(response.StatusCode,  null, Stream.Null);
        }

        Stream stream;
        try
        {
            stream = response.Content.ReadAsStream();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return new RequestResult(HttpStatusCode.InternalServerError, null, Stream.Null);
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
        if (response.RequestMessage is not null && response.RequestMessage.RequestUri is not null)
        {
            return new RequestResult(response.StatusCode, document, stream, true,
                response.RequestMessage.RequestUri.AbsoluteUri);
        }

        return new RequestResult(response.StatusCode, document, stream);
    }
}