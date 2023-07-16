using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
    protected TrangaSettings settings { get; }
    protected DownloadClient downloadClient { get; init; } = null!;

    protected readonly Logger? logger;


    protected Connector(TrangaSettings settings, Logger? logger = null)
    {
        this.settings = settings;
        this.logger = logger;
        if (!Directory.Exists(settings.coverImageCache))
            Directory.CreateDirectory(settings.coverImageCache);
    }
    
    public abstract string name { get; } //Name of the Connector (e.g. Website)

    public Publication[] GetPublications(ref HashSet<Publication> publicationCollection, string publicationTitle = "")
    {
        Publication[] ret = GetPublicationsInternal(publicationTitle);
        foreach (Publication p in ret)
            publicationCollection.Add(p);
        return ret;
    }
    
    /// <summary>
    /// Returns all Publications with the given string.
    /// If the string is empty or null, returns all Publication of the Connector
    /// </summary>
    /// <param name="publicationTitle">Search-Query</param>
    /// <returns>Publications matching the query</returns>
    protected abstract Publication[] GetPublicationsInternal(string publicationTitle = "");
    
    /// <summary>
    /// Returns all Chapters of the publication in the provided language.
    /// If the language is empty or null, returns all Chapters in all Languages.
    /// </summary>
    /// <param name="publication">Publication to get Chapters for</param>
    /// <param name="language">Language of the Chapters</param>
    /// <returns>Array of Chapters matching Publication and Language</returns>
    public abstract Chapter[] GetChapters(Publication publication, string language = "");

    /// <summary>
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <param name="collection"></param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    public List<Chapter> GetNewChaptersList(Publication publication, string language, ref HashSet<Publication> collection)
    {
        Chapter[] newChapters = this.GetChapters(publication, language);
        collection.Add(publication);
        NumberFormatInfo decimalPoint = new (){ NumberDecimalSeparator = "." };
        logger?.WriteLine(this.GetType().ToString(), "Checking for duplicates");
        List<Chapter> newChaptersList = newChapters.Where(nChapter =>
            float.Parse(nChapter.chapterNumber, decimalPoint) > publication.ignoreChaptersBelow &&
            !nChapter.CheckChapterIsDownloaded(settings.downloadLocation)).ToList();
        logger?.WriteLine(this.GetType().ToString(), $"{newChaptersList.Count} new chapters.");
        
        return newChaptersList;
    }

    public Chapter[] SelectChapters(Publication publication, string searchTerm, string? language = null)
    {
        Chapter[] availableChapters = this.GetChapters(publication, language??"en");
        Regex volumeRegex = new ("((v(ol)*(olume)*)+ *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
        Regex chapterRegex = new ("((c(h)*(hapter)*)+ *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
        Regex singleResultRegex = new("([0-9]+)", RegexOptions.IgnoreCase);
        Regex rangeResultRegex = new("([0-9]+(-[0-9]+))", RegexOptions.IgnoreCase);
        Regex allRegex = new("a(ll)?", RegexOptions.IgnoreCase);
        if (volumeRegex.IsMatch(searchTerm) && chapterRegex.IsMatch(searchTerm))
        {
            string volume = singleResultRegex.Match(volumeRegex.Match(searchTerm).Value).Value;
            string chapter = singleResultRegex.Match(chapterRegex.Match(searchTerm).Value).Value;
            return availableChapters.Where(aCh => aCh.volumeNumber is not null &&
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
                return availableChapters.Where(aCh => Convert.ToInt32(aCh.chapterNumber) >= start &&
                                                      Convert.ToInt32(aCh.chapterNumber) <= end).ToArray();
            }
            else if (singleResultRegex.IsMatch(chapter))
            {
                string chapterNumber = singleResultRegex.Match(chapter).Value;
                return availableChapters.Where(aCh =>
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
            else if (allRegex.IsMatch(searchTerm))
                return availableChapters;
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
    public abstract HttpStatusCode DownloadChapter(Publication publication, Chapter chapter, DownloadChapterTask parentTask, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Copies the already downloaded cover from cache to downloadLocation
    /// </summary>
    /// <param name="publication">Publication to retrieve Cover for</param>
    /// <param name="settings">TrangaSettings</param>
    public void CopyCoverFromCacheToDownloadLocation(Publication publication, TrangaSettings settings)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Cloning cover {publication.sortName} -> {publication.internalId}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = publication.CreatePublicationFolder(settings.downloadLocation);
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
    /// Downloads Image from URL and saves it to the given path(incl. fileName)
    /// </summary>
    /// <param name="imageUrl"></param>
    /// <param name="fullPath"></param>
    /// <param name="requestType">RequestType for Rate-Limit</param>
    /// <param name="referrer">referrer used in html request header</param>
    private HttpStatusCode DownloadImage(string imageUrl, string fullPath, byte requestType, string? referrer = null)
    {
        DownloadClient.RequestResult requestResult = downloadClient.MakeRequest(imageUrl, requestType, referrer);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.result == Stream.Null)
            return requestResult.statusCode;
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(fullPath, buffer);
        return requestResult.statusCode;
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
    /// <param name="cancellationToken"></param>
    protected HttpStatusCode DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, byte requestType, DownloadChapterTask parentTask, string? comicInfoPath = null, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        logger?.WriteLine("Connector", $"Downloading Images for {saveArchiveFilePath}");
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice.
            return HttpStatusCode.OK;
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory().FullName;

        int chapter = 0;
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            string[] split = imageUrl.Split('.');
            string extension = split[^1];
            logger?.WriteLine("Connector", $"Downloading Image {chapter + 1:000}/{imageUrls.Length:000} {parentTask.publication.sortName} {parentTask.publication.internalId} Vol.{parentTask.chapter.volumeNumber} Ch.{parentTask.chapter.chapterNumber} {parentTask.progress:P2}");
            HttpStatusCode status = DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), requestType, referrer);
            if ((int)status < 200 || (int)status >= 300)
                return status;
            parentTask.IncrementProgress(1.0 / imageUrls.Length);
            if (cancellationToken?.IsCancellationRequested ?? false)
                return HttpStatusCode.RequestTimeout;
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        logger?.WriteLine("Connector", $"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, GroupRead | GroupWrite | OtherRead | OtherWrite | UserRead | UserWrite);
        Directory.Delete(tempFolder, true); //Cleanup
        return HttpStatusCode.OK;
    }
    
    protected string SaveCoverImageToCache(string url, byte requestType)
    {
        string[] split = url.Split('/');
        string filename = split[^1];
        string saveImagePath = Path.Join(settings.coverImageCache, filename);

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
        // ReSharper disable once InconsistentNaming
        private readonly Logger? logger;

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
            if (!response.IsSuccessStatusCode)
            {
                logger?.WriteLine(this.GetType().ToString(), $"Request-Error {response.StatusCode}: {response.ReasonPhrase}");
                return new RequestResult(response.StatusCode, Stream.Null);
            }

            // Request has been redirected to another page. For example, it redirects directly to the results when there is only 1 result
            if(response.RequestMessage is not null && response.RequestMessage.RequestUri is not null)
            {
                return new RequestResult(response.StatusCode, response.Content.ReadAsStream(), true, response.RequestMessage.RequestUri.AbsoluteUri);
            }

            return new RequestResult(response.StatusCode, response.Content.ReadAsStream());
        }

        public struct RequestResult
        {
            public HttpStatusCode statusCode { get; }
            public Stream result { get; }
            public bool HasBeenRedirected { get; }
            public string? RedirectedToUrl { get; }

            public RequestResult(HttpStatusCode statusCode, Stream result)
            {
                this.statusCode = statusCode;
                this.result = result;
            }

            public RequestResult(HttpStatusCode statusCode, Stream result, bool hasBeenRedirected, string redirectedTo)
                : this(statusCode, result)
            {
                this.HasBeenRedirected = hasBeenRedirected;
                RedirectedToUrl = redirectedTo;
            }
        }
    }
}