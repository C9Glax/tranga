namespace API.MangaDownloadClients;

public interface IDownloadClient
{
    internal Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null,
        CancellationToken? cancellationToken = null);
}