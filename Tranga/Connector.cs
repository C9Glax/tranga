using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Logging;

namespace Tranga;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class Connector
{
    internal string downloadLocation { get; }  //Location of local files
    protected DownloadClient downloadClient { get; init; }

    protected Logger? logger;

    protected string imageCachePath;

    protected Connector(string downloadLocation, string imageCachePath, Logger? logger)
    {
        this.downloadLocation = downloadLocation;
        this.logger = logger;
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            //RequestTypes for RateLimits
        }, logger);
        this.imageCachePath = imageCachePath;
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
    /// <param name="settings">TrangaSettings</param>
    public abstract void CloneCoverFromCache(Publication publication, TrangaSettings settings);

    /// <summary>
    /// Saves the series-info to series.json in the Publication Folder
    /// </summary>
    /// <param name="publication">Publication to save series.json for</param>
    public void SaveSeriesInfo(Publication publication)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Saving series.json for {publication.sortName}");
        //Check if Publication already has a Folder and a series.json
        string publicationFolder = Path.Join(downloadLocation, publication.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        
        string seriesInfoPath = Path.Join(publicationFolder, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,publication.GetSeriesInfoJson());
    }

    /// <summary>
    /// Creates a string containing XML of publication and chapter.
    /// See ComicInfo.xml
    /// </summary>
    /// <returns>XML-string</returns>
    protected static string CreateComicInfo(Publication publication, Chapter chapter, Logger? logger)
    {
        logger?.WriteLine("Connector", $"Creating ComicInfo.Xml for {publication.sortName} {publication.internalId} {chapter.volumeNumber}-{chapter.chapterNumber}");
        XElement comicInfo = new XElement("ComicInfo",
            new XElement("Tags", string.Join(',',publication.tags)),
            new XElement("LanguageISO", publication.originalLanguage),
            new XElement("Title", chapter.name),
            new XElement("Writer", publication.author),
            new XElement("Volume", chapter.volumeNumber),
            new XElement("Number", chapter.chapterNumber) //TODO check if this is correct at some point
        );
        return comicInfo.ToString();
    }

    /// <summary>
    /// Checks if a chapter-archive is already present
    /// </summary>
    /// <returns>true if chapter is present</returns>
    public bool ChapterIsDownloaded(Publication publication, Chapter chapter)
    {
        return File.Exists(CreateFullFilepath(publication, chapter));
    }

    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    protected string CreateFullFilepath(Publication publication, Chapter chapter)
    {
        return Path.Join(downloadLocation, publication.folderName, $"{chapter.fileName}.cbz");
    }

    /// <summary>
    /// Downloads Image from URL and saves it to the given path(incl. fileName)
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <param name="fullPath"></param>
    /// <param name="downloadClient">DownloadClient of the connector</param>
    /// <param name="requestType">Requesttype for ratelimit</param>
    protected static void DownloadImage(string imageUrl, string fullPath, DownloadClient downloadClient, byte requestType)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl, requestType);
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
    /// <param name="logger"></param>
    /// <param name="comicInfoPath">Path of the generate Chapter ComicInfo.xml, if it was generated</param>
    /// <param name="requestType">RequestType for RateLimits</param>
    protected static void DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, DownloadClient downloadClient, byte requestType, Logger? logger, string? comicInfoPath = null)
    {
        logger?.WriteLine("Connector", $"Downloading Images for {saveArchiveFilePath}");
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice.
            return;
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory().FullName;

        int chapter = 0;
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            string[] split = imageUrl.Split('.');
            string extension = split[^1];
            logger?.WriteLine("Connector", $"Downloading Image {chapter + 1}/{imageUrls.Length}");
            DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), downloadClient, requestType);
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        logger?.WriteLine("Connector", $"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        Directory.Delete(tempFolder, true); //Cleanup
    }
    
    protected class DownloadClient
    {
        private static readonly HttpClient Client = new();

        private readonly Dictionary<byte, DateTime> _lastExecutedRateLimit;
        private readonly Dictionary<byte, TimeSpan> _rateLimit;
        private Logger? logger;

        /// <summary>
        /// Creates a httpClient
        /// </summary>
        /// <param name="delay">minimum delay between requests (to avoid spam)</param>
        /// <param name="rateLimitRequestsPerMinute">Rate limits for requests. byte is RequestType, int maximum requests per minute for RequestType</param>
        public DownloadClient(Dictionary<byte, int> rateLimitRequestsPerMinute, Logger? logger)
        {
            this.logger = logger;
            _lastExecutedRateLimit = new();
            _rateLimit = new();
            foreach(KeyValuePair<byte, int> limit in rateLimitRequestsPerMinute)
                _rateLimit.Add(limit.Key, TimeSpan.FromMinutes(1).Divide(limit.Value));
        }

        /// <summary>
        /// Request Webpage
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestType">For RateLimits: Same Endpoints use same type</param>
        /// <returns>RequestResult with StatusCode and Stream of received data</returns>
        public RequestResult MakeRequest(string url, byte requestType)
        {
            if (_rateLimit.TryGetValue(requestType, out TimeSpan value))
                _lastExecutedRateLimit.TryAdd(requestType, DateTime.Now.Subtract(value));
            else
            {
                logger?.WriteLine(this.GetType().ToString(), "RequestType not configured for rate-limit.");
                return new RequestResult(HttpStatusCode.NotAcceptable, Stream.Null);
            }

            TimeSpan rateLimitTimeout = _rateLimit[requestType]
                .Subtract(DateTime.Now.Subtract(_lastExecutedRateLimit[requestType]));
            
            if(rateLimitTimeout > TimeSpan.Zero)
                Thread.Sleep(rateLimitTimeout);

            HttpResponseMessage? response = null;
            while (response is null)
            {
                try
                {
                    HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
                    _lastExecutedRateLimit[requestType] = DateTime.Now;
                    response = Client.Send(requestMessage);
                }
                catch (HttpRequestException e)
                {
                    logger?.WriteLine(this.GetType().ToString(), e.Message);
                    logger?.WriteLine(this.GetType().ToString(), $"Waiting {_rateLimit[requestType] * 2}");
                    Thread.Sleep(_rateLimit[requestType] * 2);
                }
            }
            Stream resultString = response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            if (!response.IsSuccessStatusCode)
                logger?.WriteLine(this.GetType().ToString(), $"Request-Error {response.StatusCode}: {response.ReasonPhrase}");
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