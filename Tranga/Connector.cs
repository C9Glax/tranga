using System.IO.Compression;
using System.Net;

namespace Tranga;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class Connector
{
    internal string downloadLocation { get; }  //Location of local files
    protected DownloadClient downloadClient { get; }

    protected Connector(string downloadLocation, uint downloadDelay)
    {
        this.downloadLocation = downloadLocation;
        this.downloadClient = new DownloadClient(downloadDelay);
    }
    
    public abstract string name { get; } //Name of the Connector (e.g. Website)
    
    /// <summary>
    /// Returns all Publications with the given string.
    /// If the string is empty or null, returns all Publication of the Connector
    /// </summary>
    /// <param name="publicationTitle">Search-Query</param>
    /// <returns>Publications matching the query</returns>
    public abstract Publication[] GetPublications(string publicationTitle = "");
    
    /// <summary>
    /// Returns all Chapters of the publication in the provided language.
    /// If the language is empty or null, returns all Chapters in all Languages.
    /// </summary>
    /// <param name="publication">Publication to get Chapters for</param>
    /// <param name="language">Language of the Chapters</param>
    /// <returns>Array of Chapters matching Publication and Language</returns>
    public abstract Chapter[] GetChapters(Publication publication, string language = "");
    
    /// <summary>
    /// Retrieves the Chapter (+Images) from the website.
    /// Should later call DownloadChapterImages to retrieve the individual Images of the Chapter.
    /// </summary>
    /// <param name="publication">Publication that contains Chapter</param>
    /// <param name="chapter">Chapter with Images to retrieve</param>
    public abstract void DownloadChapter(Publication publication, Chapter chapter);
    
    /// <summary>
    /// Retrieves the Cover from the Website
    /// </summary>
    /// <param name="publication">Publication to retrieve Cover for</param>
    public abstract void DownloadCover(Publication publication);

    /// <summary>
    /// Saves the series-info to series.json in the Publication Folder
    /// </summary>
    /// <param name="publication">Publication to save series.json for</param>
    public void SaveSeriesInfo(Publication publication)
    {
        string seriesInfoPath = Path.Join(downloadLocation, publication.folderName, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,publication.GetSeriesInfo());
    }
    
    protected static void DownloadImage(string imageUrl, string fullPath, DownloadClient downloadClient)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl);
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(fullPath, buffer);
    }
    
    protected static void DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, DownloadClient downloadClient)
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