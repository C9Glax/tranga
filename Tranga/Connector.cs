using System.IO.Compression;
using System.Net;
using System.Xml.Linq;

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
        //Check if Publication already has a Folder and a series.json
        string publicationFolder = Path.Join(downloadLocation, publication.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        
        string seriesInfoPath = Path.Join(publicationFolder, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,publication.GetSeriesInfoJson());
    }

    protected static string CreateComicInfo(Publication publication, Chapter chapter)
    {
        XElement comicInfo = new XElement("ComicInfo",
            new XElement("Tags", string.Join(',',publication.tags)),
            new XElement("LanguageISO", publication.originalLanguage),
            new XElement("Title", chapter.name)
        );
        return comicInfo.ToString();
    }

    public bool ChapterIsDownloaded(Publication publication, Chapter chapter)
    {
        return File.Exists(CreateFullFilepath(publication, chapter));
    }

    protected string CreateFullFilepath(Publication publication, Chapter chapter)
    {
        return Path.Join(downloadLocation, publication.folderName, chapter.fileName);
    }
    
    /// <summary>
    /// Downloads Image from URL and saves it to the given path(incl. fileName)
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <param name="fullPath"></param>
    /// <param name="downloadClient">DownloadClient of the connector</param>
    protected static void DownloadImage(string imageUrl, string fullPath, DownloadClient downloadClient)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl);
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(fullPath, buffer);
    }
    
    /// <summary>
    /// Downloads all Images from URLs, Compresses to zip(cbz) and saves.
    /// </summary>
    /// <param name="imageUrls">List of URLs to download Images from</param>
    /// <param name="saveArchiveFilePath">Full path to save archive to (without file ending .cbz)</param>
    /// <param name="downloadClient">DownloadClient of the connector</param>
    /// <param name="comicInfoPath">Path of the generate Chapter ComicInfo.xml, if it was generated</param>
    protected static void DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, DownloadClient downloadClient, string? comicInfoPath = null)
    {
        //Check if Publication Directory already exists
        string[] splitPath = saveArchiveFilePath.Split(Path.DirectorySeparatorChar);
        string directoryPath = Path.Combine(splitPath.Take(splitPath.Length - 1).ToArray());
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        string fullPath = $"{saveArchiveFilePath}.cbz";
        if (File.Exists(fullPath)) //Don't download twice.
            return;
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory().FullName;

        int chapter = 0;
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            string[] split = imageUrl.Split('.');
            string extension = split[^1];
            DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), downloadClient);
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, fullPath);
        Directory.Delete(tempFolder, true); //Cleanup
    }
    
    protected class DownloadClient
    {
        private readonly TimeSpan _requestSpeed;
        private DateTime _lastRequest;
        private static readonly HttpClient Client = new();

        /// <summary>
        /// Creates a httpClient
        /// </summary>
        /// <param name="delay">minimum delay between requests (to avoid spam)</param>
        public DownloadClient(uint delay)
        {
            _requestSpeed = TimeSpan.FromMilliseconds(delay);
            _lastRequest = DateTime.Now.Subtract(_requestSpeed);
        }
        
        /// <summary>
        /// Request Webpage
        /// </summary>
        /// <param name="url"></param>
        /// <returns>RequestResult with StatusCode and Stream of received data</returns>
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