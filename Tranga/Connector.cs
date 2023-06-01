using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Logging;
using static System.IO.UnixFileMode;

namespace Tranga;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class Connector
{
    internal string downloadLocation { get; }  //Location of local files
    protected DownloadClient downloadClient { get; init; }

    protected readonly Logger? logger;

    protected readonly string imageCachePath;

    protected Connector(string downloadLocation, string imageCachePath, Logger? logger)
    {
        this.downloadLocation = downloadLocation;
        this.logger = logger;
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            //RequestTypes for RateLimits
        }, logger);
        this.imageCachePath = imageCachePath;
        if (!Directory.Exists(imageCachePath))
            Directory.CreateDirectory(this.imageCachePath);
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
    /// Should later call DownloadChapterImages to retrieve the individual Images of the Chapter and create .cbz archive.
    /// </summary>
    /// <param name="publication">Publication that contains Chapter</param>
    /// <param name="chapter">Chapter with Images to retrieve</param>
    /// <param name="parentTask">Will be used for progress-tracking</param>
    public abstract void DownloadChapter(Publication publication, Chapter chapter, TrangaTask parentTask);

    /// <summary>
    /// Copies the already downloaded cover from cache to downloadLocation
    /// </summary>
    /// <param name="publication">Publication to retrieve Cover for</param>
    /// <param name="settings">TrangaSettings</param>
    public void CopyCoverFromCacheToDownloadLocation(Publication publication, TrangaSettings settings)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Cloning cover {publication.sortName} {publication.internalId}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = Path.Join(downloadLocation, publication.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover.")))
        {
            logger?.WriteLine(this.GetType().ToString(), $"Cover exists {publication.sortName}");
            return;
        }

        string fileInCache = Path.Join(settings.coverImageCache, publication.coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        logger?.WriteLine(this.GetType().ToString(), $"Cloning cover {fileInCache} -> {newFilePath}");
        File.Copy(fileInCache, newFilePath, true);
    }

    /// <summary>
    /// Creates a string containing XML of publication and chapter.
    /// See ComicInfo.xml
    /// </summary>
    /// <returns>XML-string</returns>
    protected static string GetComicInfoXmlString(Publication publication, Chapter chapter, Logger? logger)
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
    public bool CheckChapterIsDownloaded(Publication publication, Chapter chapter)
    {
        return File.Exists(GetArchiveFilePath(publication, chapter));
    }

    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    protected string GetArchiveFilePath(Publication publication, Chapter chapter)
    {
        return Path.Join(downloadLocation, publication.folderName, $"{chapter.fileName}.cbz");
    }

    /// <summary>
    /// Downloads Image from URL and saves it to the given path(incl. fileName)
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <param name="fullPath"></param>
    /// <param name="requestType">RequestType for Rate-Limit</param>
    /// <param name="referrer">referrer used in html request header</param>
    private void DownloadImage(string imageUrl, string fullPath, byte requestType, string? referrer = null)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl, requestType, referrer);
        if (requestResult.result != Stream.Null)
        {
            byte[] buffer = new byte[requestResult.result.Length];
            requestResult.result.ReadExactly(buffer, 0, buffer.Length);
            File.WriteAllBytes(fullPath, buffer);
        }else
            logger?.WriteLine(this.GetType().ToString(), "No Stream-Content in result.");
    }

    /// <summary>
    /// Downloads all Images from URLs, Compresses to zip(cbz) and saves.
    /// </summary>
    /// <param name="imageUrls">List of URLs to download Images from</param>
    /// <param name="saveArchiveFilePath">Full path to save archive to (without file ending .cbz)</param>
    /// <param name="parentTask">Used for progress tracking</param>
    /// <param name="comicInfoPath">Path of the generate Chapter ComicInfo.xml, if it was generated</param>
    /// <param name="requestType">RequestType for RateLimits</param>
    /// <param name="referrer">Used in http request header</param>
    protected void DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, byte requestType, TrangaTask parentTask, string? comicInfoPath = null, string? referrer = null)
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
            logger?.WriteLine("Connector", $"Downloading Image {chapter + 1}/{imageUrls.Length} {parentTask.publication?.sortName,20} {parentTask.publication?.internalId,20} Total Task Progress: {parentTask.progress:00.0}%");
            DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), requestType, referrer);
            parentTask.tasksFinished++;
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        logger?.WriteLine("Connector", $"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
        Directory.Delete(tempFolder, true); //Cleanup
    }
    
    protected string SaveCoverImageToCache(string url, byte requestType)
    {
        string[] split = url.Split('/');
        string filename = split[^1];
        string saveImagePath = Path.Join(imageCachePath, filename);

        if (File.Exists(saveImagePath))
            return filename;
        
        DownloadClient.RequestResult coverResult = downloadClient.MakeRequest(url, requestType);
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        logger?.WriteLine(this.GetType().ToString(), $"Saving image to {saveImagePath}");
        return filename;
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
        /// <param name="rateLimitRequestsPerMinute">Rate limits for requests. byte is RequestType, int maximum requests per minute for RequestType</param>
        /// <param name="logger"></param>
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
        /// <param name="referrer">Used in http request header</param>
        /// <returns>RequestResult with StatusCode and Stream of received data</returns>
        public RequestResult MakeRequest(string url, byte requestType, string? referrer = null)
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
                    if(referrer is not null)
                        requestMessage.Headers.Referrer = new Uri(referrer);
                    _lastExecutedRateLimit[requestType] = DateTime.Now;
                    response = Client.Send(requestMessage);
                }
                catch (HttpRequestException e)
                {
                    logger?.WriteLine(this.GetType().ToString(), e.Message);
                    logger?.WriteLine(this.GetType().ToString(), $"Waiting {_rateLimit[requestType] * 2}... Retrying.");
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