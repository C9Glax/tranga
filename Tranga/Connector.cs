using System.IO.Compression;
using System.Net;

namespace Tranga;

public abstract class Connector
{
    internal abstract string downloadLocation { get; }
    public abstract string name { get; }
    public abstract Publication[] GetPublications(string publicationTitle = "");
    public abstract Chapter[] GetChapters(Publication publication);
    public abstract void DownloadChapter(Publication publication, Chapter chapter); //where to?
    internal abstract void DownloadImage(string url, string path);

    internal void DownloadChapterImage(string url, string outputFolder)
    {
        string tempFolder = Path.GetTempFileName();
        File.Delete(tempFolder);
        Directory.CreateDirectory(tempFolder);
        
        DownloadImage(url, tempFolder);
        ZipFile.CreateFromDirectory(tempFolder, $"{outputFolder}.cbz");
    }

    internal class DownloadClient
    {
        static readonly HttpClient client = new HttpClient();
        public RequestResult GetPage(string url)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response = client.Send(requestMessage);
            Stream resultString = response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            return new RequestResult(response.StatusCode, resultString);
        }

        public struct RequestResult
        {
            public HttpStatusCode statusCode { get; }
            public Stream result { get; }

            public RequestResult(HttpStatusCode statusCode, Stream result)
            {
                this.statusCode = statusCode;
                this.result = result;
            }
        }
    }
}