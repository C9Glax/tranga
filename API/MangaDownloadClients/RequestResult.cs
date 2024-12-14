using System.Net;
using HtmlAgilityPack;

namespace API.MangaDownloadClients;

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