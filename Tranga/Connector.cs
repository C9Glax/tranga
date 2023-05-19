using System.IO.Compression;
using System.Net;

namespace Tranga;

public abstract class Connector
{
    internal string downloadLocation { get; }  //Location of local files
    protected DownloadClient downloadClient { get; }

    protected Connector(string downloadLocation, uint downloadDelay)
    {
        this.downloadLocation = downloadLocation;
        this.downloadClient = new DownloadClient(downloadDelay);
    }
    
    internal string downloadLocation { get; }
    public abstract string name { get; }
    public abstract Publication[] GetPublications(string publicationTitle = "");
    public abstract Chapter[] GetChapters(Publication publication, string language = "");
    public abstract void DownloadChapter(Publication publication, Chapter chapter); //where to?
    protected abstract void DownloadImage(string url, string savePath);
    public abstract void DownloadCover(Publication publication);

    protected void DownloadImage(string imageUrl, string fullPath, DownloadClient downloadClient)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl);
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(fullPath, buffer);
    }
    
    protected void DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, DownloadClient downloadClient)
    {
        string[] splitPath = saveArchiveFilePath.Split(Path.DirectorySeparatorChar);
        string directoryPath = Path.Combine(splitPath.Take(splitPath.Length - 1).ToArray());
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        string fullPath = $"{saveArchiveFilePath}.cbz";
        if (File.Exists(fullPath))
            return;
        
        string tempFolder = Path.GetTempFileName();
        File.Delete(tempFolder);
        Directory.CreateDirectory(tempFolder);

        int chapter = 0;
        foreach (string imageUrl in imageUrls)
        {
            string[] split = imageUrl.Split('.');
            string extension = split[split.Length - 1];
            DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), downloadClient);
        }
        
        ZipFile.CreateFromDirectory(tempFolder, fullPath);
    }

    public void SaveSeriesInfo(Publication publication)
    {
        string seriesInfoPath = Path.Join(downloadLocation, publication.folderName, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,publication.GetSeriesInfo());
    }

    protected class DownloadClient
    {
        private readonly TimeSpan _requestSpeed;
        private DateTime _lastRequest;
        private static readonly HttpClient Client = new();

        public DownloadClient(uint delay)
        {
            _requestSpeed = TimeSpan.FromMilliseconds(delay);
            _lastRequest = DateTime.Now.Subtract(_requestSpeed);
        }
        
        public RequestResult MakeRequest(string url)
        {
            while((DateTime.Now - _lastRequest) < _requestSpeed)
                Thread.Sleep(10);
            _lastRequest = DateTime.Now;

            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            HttpResponseMessage response = Client.Send(requestMessage);
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