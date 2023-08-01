using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.IO.UnixFileMode;

namespace Tranga.Connectors;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class Connector : TBaseObject
{
    internal DownloadClient downloadClient { get; init; } = null!;

    protected Connector(TBaseObject clone) : base(clone)
    {
        if (!Directory.Exists(settings.coverImageCache))
            Directory.CreateDirectory(settings.coverImageCache);
    }
    
    public abstract string name { get; } //Name of the Connector (e.g. Website)

    /// <summary>
    /// Returns all Publications with the given string.
    /// If the string is empty or null, returns all Publication of the Connector
    /// </summary>
    /// <param name="publicationTitle">Search-Query</param>
    /// <returns>Publications matching the query</returns>
    protected abstract Publication[] GetPublications(string publicationTitle = "");
    
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
        Log($"Getting new Chapters for {publication}");
        Chapter[] newChapters = this.GetChapters(publication, language);
        collection.Add(publication);
        NumberFormatInfo decimalPoint = new (){ NumberDecimalSeparator = "." };
        Log($"Checking for duplicates {publication}");
        List<Chapter> newChaptersList = newChapters.Where(nChapter =>
            float.Parse(nChapter.chapterNumber, decimalPoint) > publication.ignoreChaptersBelow &&
            !nChapter.CheckChapterIsDownloaded(settings.downloadLocation)).ToList();
        Log($"{newChaptersList.Count} new chapters. {publication}");
        
        return newChaptersList;
    }

    public Chapter[] SelectChapters(Publication publication, string searchTerm, string? language = null)
    {
        Chapter[] availableChapters = this.GetChapters(publication, language??"en");
        Regex volumeRegex = new ("((v(ol)*(olume)*){1} *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
        Regex chapterRegex = new ("((c(h)*(hapter)*){1} *([0-9]+(-[0-9]+)?){1})", RegexOptions.IgnoreCase);
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
            string chapter = chapterRegex.Match(searchTerm).Value;
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
    /// <param name="cancellationToken"></param>
    public abstract HttpStatusCode DownloadChapter(Publication publication, Chapter chapter, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Copies the already downloaded cover from cache to downloadLocation
    /// </summary>
    /// <param name="publication">Publication to retrieve Cover for</param>
    public void CopyCoverFromCacheToDownloadLocation(Publication publication)
    {
        Log($"Copy cover {publication}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = publication.CreatePublicationFolder(settings.downloadLocation);
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            Log($"Cover exists {publication}");
            return;
        }

        string fileInCache = Path.Join(settings.coverImageCache, publication.coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        Log($"Cloning cover {fileInCache} -> {newFilePath}");
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
    /// <param name="comicInfoPath">Path of the generate Chapter ComicInfo.xml, if it was generated</param>
    /// <param name="requestType">RequestType for RateLimits</param>
    /// <param name="referrer">Used in http request header</param>
    /// <param name="cancellationToken"></param>
    protected HttpStatusCode DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, byte requestType, string? comicInfoPath = null, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Log($"Downloading Images for {saveArchiveFilePath}");
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
            Log($"Downloading image {chapter + 1:000}/{imageUrls.Length:000}"); //TODO
            HttpStatusCode status = DownloadImage(imageUrl, Path.Join(tempFolder, $"{chapter++}.{extension}"), requestType, referrer);
            if ((int)status < 200 || (int)status >= 300)
                return status;
            if (cancellationToken?.IsCancellationRequested ?? false)
                return HttpStatusCode.RequestTimeout;
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        Log($"Creating archive {saveArchiveFilePath}");
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
        Log($"Saving cover to {saveImagePath}");
        return filename;
    }
}