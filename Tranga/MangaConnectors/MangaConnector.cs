using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Tranga.Jobs;
using static System.IO.UnixFileMode;

namespace Tranga.MangaConnectors;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class MangaConnector : GlobalBase
{
    internal DownloadClient downloadClient { get; init; } = null!;

    public void StopDownloadClient()
    {
        downloadClient.Close();
    }

    protected MangaConnector(GlobalBase clone, string name) : base(clone)
    {
        this.name = name;
        Directory.CreateDirectory(settings.coverImageCache);
    }
    
    public string name { get; } //Name of the Connector (e.g. Website)

    /// <summary>
    /// Returns all Publications with the given string.
    /// If the string is empty or null, returns all Publication of the Connector
    /// </summary>
    /// <param name="publicationTitle">Search-Query</param>
    /// <returns>Publications matching the query</returns>
    public abstract Manga[] GetManga(string publicationTitle = "");

    public abstract Manga? GetMangaFromUrl(string url);
    
    /// <summary>
    /// Returns all Chapters of the publication in the provided language.
    /// If the language is empty or null, returns all Chapters in all Languages.
    /// </summary>
    /// <param name="manga">Publication to get Chapters for</param>
    /// <param name="language">Language of the Chapters</param>
    /// <returns>Array of Chapters matching Publication and Language</returns>
    public abstract Chapter[] GetChapters(Manga manga, string language="en");

    /// <summary>
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="manga">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    public Chapter[] GetNewChapters(Manga manga, string language = "en")
    {
        Log($"Getting new Chapters for {manga}");
        Chapter[] newChapters = this.GetChapters(manga, language);
        Log($"Checking for duplicates {manga}");
        List<Chapter> newChaptersList = newChapters.Where(nChapter =>
            float.Parse(nChapter.chapterNumber, numberFormatDecimalPoint) > manga.ignoreChaptersBelow &&
            !nChapter.CheckChapterIsDownloaded(settings.downloadLocation)).ToList();
        Log($"{newChaptersList.Count} new chapters. {manga}");
        
        return newChaptersList.ToArray();
    }

    public Chapter[] SelectChapters(Manga manga, string searchTerm, string? language = null)
    {
        Chapter[] availableChapters = this.GetChapters(manga, language??"en");
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
    
    public abstract HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null);

    /// <summary>
    /// Copies the already downloaded cover from cache to downloadLocation
    /// </summary>
    /// <param name="manga">Publication to retrieve Cover for</param>
    public void CopyCoverFromCacheToDownloadLocation(Manga manga)
    {
        Log($"Copy cover {manga}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = manga.CreatePublicationFolder(settings.downloadLocation);
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            Log($"Cover exists {manga}");
            return;
        }

        string fileInCache = Path.Join(settings.coverImageCache, manga.coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        Log($"Cloning cover {fileInCache} -> {newFilePath}");
        File.Copy(fileInCache, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite);
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
        
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return requestResult.statusCode;
        if (requestResult.result == Stream.Null)
            return HttpStatusCode.NotFound;

        FileStream fs = new (fullPath, FileMode.Create);
        requestResult.result.CopyTo(fs);
        fs.Close();
        return requestResult.statusCode;
    }

    protected HttpStatusCode DownloadChapterImages(string[] imageUrls, string saveArchiveFilePath, byte requestType, string? comicInfoPath = null, string? referrer = null, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Log($"Downloading Images for {saveArchiveFilePath}");
        if(progressToken is not null)
            progressToken.increments = imageUrls.Length;
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Directory.CreateDirectory(directoryPath,
                    UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );
            else
                Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice.
            return HttpStatusCode.Created;
        
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
            {
                progressToken?.Complete();
                return status;
            }
            if (progressToken?.cancellationRequested ?? false)
            {
                progressToken?.Complete();
                return HttpStatusCode.RequestTimeout;
            }
            progressToken?.Increment();
        }
        
        if(comicInfoPath is not null)
            File.Copy(comicInfoPath, Path.Join(tempFolder, "ComicInfo.xml"));
        
        Log($"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        progressToken?.Complete();
        return HttpStatusCode.OK;
    }
    
    protected string SaveCoverImageToCache(string url, byte requestType)
    {
        string filename = url.Split('/')[^1].Split('?')[0];
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