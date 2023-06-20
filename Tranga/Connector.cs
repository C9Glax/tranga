using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Logging;
using Tranga.TrangaTasks;
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

    public Chapter[] SearchChapters(Publication publication, string searchTerm, string? language = null)
    {
        Chapter[] availableChapters = this.GetChapters(publication, language??"en");
        Regex volumeRegex = new ("((v(ol)*(olume)*)+ *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
        Regex chapterRegex = new ("((c(h)*(hapter)*)+ *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
        Regex singleResultRegex = new("([0-9]+)", RegexOptions.IgnoreCase);
        Regex rangeResultRegex = new("([0-9]+(-[0-9]+))", RegexOptions.IgnoreCase);
        if (volumeRegex.IsMatch(searchTerm) && chapterRegex.IsMatch(searchTerm))
        {
            string volume = singleResultRegex.Match(volumeRegex.Match(searchTerm).Value).Value;
            string chapter = singleResultRegex.Match(chapterRegex.Match(searchTerm).Value).Value;
            return availableChapters.Where(aCh => aCh.volumeNumber is not null && aCh.chapterNumber is not null &&
                aCh.volumeNumber.Equals(volume, StringComparison.InvariantCultureIgnoreCase) &&
                aCh.chapterNumber.Equals(chapter, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
        }
        else if (volumeRegex.IsMatch(searchTerm))
        {
            string volume = volumeRegex.Match(searchTerm).Value;
            if (rangeResultRegex.IsMatch(volume))
            {
                string range = rangeResultRegex.Match(volume).Value;
                int start = Convert.ToInt32(range.Split('-')[0]);
                int end = Convert.ToInt32(range.Split('-')[1]);
                return availableChapters.Where(aCh => aCh.volumeNumber is not null &&
                                                      Convert.ToInt32(aCh.volumeNumber) >= start &&
                                                      Convert.ToInt32(aCh.volumeNumber) <= end).ToArray();
            }
            else if (singleResultRegex.IsMatch(volume))
            {
                string volumeNumber = singleResultRegex.Match(volume).Value;
                return availableChapters.Where(aCh =>
                    aCh.volumeNumber is not null &&
                    aCh.volumeNumber.Equals(volumeNumber, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            }

        }
        else if (chapterRegex.IsMatch(searchTerm))
        {
            string chapter = volumeRegex.Match(searchTerm).Value;
            if (rangeResultRegex.IsMatch(chapter))
            {
                string range = rangeResultRegex.Match(chapter).Value;
                int start = Convert.ToInt32(range.Split('-')[0]);
                int end = Convert.ToInt32(range.Split('-')[1]);
                return availableChapters.Where(aCh => aCh.chapterNumber is not null &&
                                                      Convert.ToInt32(aCh.chapterNumber) >= start &&
                                                      Convert.ToInt32(aCh.chapterNumber) <= end).ToArray();
            }
            else if (singleResultRegex.IsMatch(chapter))
            {
                string chapterNumber = singleResultRegex.Match(chapter).Value;
                return availableChapters.Where(aCh =>
                    aCh.chapterNumber is not null &&
                    aCh.chapterNumber.Equals(chapterNumber, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            }
        }
        else
        {
            if (rangeResultRegex.IsMatch(searchTerm))
            {
                int start = Convert.ToInt32(searchTerm.Split('-')[0]);
                int end = Convert.ToInt32(searchTerm.Split('-')[1]);
                return availableChapters[start..(end + 1)];
            }
            else if(singleResultRegex.IsMatch(searchTerm))
                return new [] { availableChapters[Convert.ToInt32(searchTerm)] };
        }

        return Array.Empty<Chapter>();
    }
    
    /// <summary>
    /// Retrieves the Chapter (+Images) from the website.
    /// Should later call DownloadChapterImages to retrieve the individual Images of the Chapter and create .cbz archive.
    /// </summary>
    /// <param name="publication">Publication that contains Chapter</param>
    /// <param name="chapter">Chapter with Images to retrieve</param>
    /// <param name="parentTask">Will be used for progress-tracking</param>
    /// <param name="cancellationToken"></param>
    public abstract bool DownloadChapter(Publication publication, Chapter chapter, DownloadChapterTask parentTask, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Copies the already downloaded cover from cache to downloadLocation
    /// </summary>
    /// <param name="publication">Publication to retrieve Cover for</param>
    /// <param name="settings">TrangaSettings</param>
    public void CopyCoverFromCacheToDownloadLocation(Publication publication, TrangaSettings settings)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Cloning cover {publication.sortName} -> {publication.internalId}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = publication.CreatePublicationFolder(downloadLocation);
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            logger?.WriteLine(this.GetType().ToString(), $"Cover exists {publication.sortName}");
            return;
        }

        string fileInCache = Path.Join(settings.coverImageCache, publication.coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        logger?.WriteLine(this.GetType().ToString(), $"Cloning cover {fileInCache} -> {newFilePath}");
        File.Copy(fileInCache, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
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
            new XElement("Writer", string.Join(',', publication.authors)),
            new XElement("Volume", chapter.volumeNumber),
            new XElement("Number", chapter.chapterNumber)
        );
        return comicInfo.ToString();
    }

    /// <summary>
    /// Checks if a chapter-archive is already present
    /// </summary>
    /// <returns>true if chapter is present</returns>
    public bool CheckChapterIsDownloaded(Publication publication, Chapter chapter)
    {
        Regex legalCharacters = new Regex(@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");
        string oldFilePath = Path.Join(downloadLocation, publication.folderName, $"{string.Concat(legalCharacters.Matches(chapter.name ?? ""))} - V{chapter.volumeNumber}C{chapter.chapterNumber} - {chapter.sortNumber}.cbz");
        string oldFilePath2 = Path.Join(downloadLocation, publication.folderName, $"{string.Concat(legalCharacters.Matches(chapter.name ?? ""))} - VC{chapter.chapterNumber} - {chapter.chapterNumber}.cbz");
        string newFilePath = GetArchiveFilePath(publication, chapter);
        if (File.Exists(oldFilePath))
            File.Move(oldFilePath, newFilePath);
        else if (File.Exists(oldFilePath2))
            File.Move(oldFilePath2, newFilePath);
        return File.Exists(newFilePath);
    }

    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    protected string GetArchiveFilePath(Publication publication, Chapter chapter)
    {
        return Path.Join(downloadLocation, publication.folderName, $"{publication.folderName} - {chapter.fileName}.cbz");
    }

    /// <summary>
    /// Downloads Image from URL and saves it to the given path(incl. fileName)
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <param name="fullPath"></param>
    /// <param name="requestType">RequestType for Rate-Limit</param>
    /// <param name="referrer">referrer used in html request header</param>
    private bool DownloadImage(string imageUrl, string fullPath, byte requestType, string? referrer = null)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl, requestType, referrer);
        if (!requestResult.success || requestResult.result == Stream.Null)
            return false;
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(fullPath, buffer);
        return true;
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
    protected bool DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, byte requestType, DownloadChapterTask parentTask, string? comicInfoPath = null, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return false;
        logger?.WriteLine("Connector", $"Downloading Images for {saveArchiveFilePath}");
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice.
            return false;
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory().FullName;

        int chapter = 0;
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            string[] split = imageUrl.Split('.');
            string extension = split[^1];
            logger?.WriteLine("Connector", $"Downloading Image {chapter + 1:000}/{imageUrls.Length:000} {parentTask.publication.sortName} {parentTask.publication.internalId} Vol.{parentTask.chapter.volumeNumber} Ch.{parentTask.chapter.chapterNumber} {parentTask.progress:P2}");
            if (!DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), requestType, referrer))
                return false;
            parentTask.IncrementProgress(1.0 / imageUrls.Length);
            if (cancellationToken?.IsCancellationRequested??false)
                return false;
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        logger?.WriteLine("Connector", $"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
        Directory.Delete(tempFolder, true); //Cleanup
        return true;
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
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

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
                return new RequestResult(false, Stream.Null);
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
            if (!response.IsSuccessStatusCode)
            {
                logger?.WriteLine(this.GetType().ToString(), $"Request-Error {response.StatusCode}: {response.ReasonPhrase}");
                return new RequestResult(false, Stream.Null);
            }
            return new RequestResult(true, response.Content.ReadAsStream());
        }

        public struct RequestResult
        {
            public bool success { get; }
            public Stream result { get; }

            public RequestResult(bool success, Stream result)
            {
                this.success = success;
                this.result = result;
            }
        }
    }
}