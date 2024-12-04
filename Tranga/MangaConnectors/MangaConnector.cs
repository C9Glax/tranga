using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
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
    public string[] SupportedLanguages;
    public string[] BaseUris;

    protected MangaConnector(GlobalBase clone, string name, string[] supportedLanguages, string[] baseUris) : base(clone)
    {
        this.name = name;
        this.SupportedLanguages = supportedLanguages;
        this.BaseUris = baseUris;
        Directory.CreateDirectory(TrangaSettings.coverImageCache);
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

    public abstract Manga? GetMangaFromId(string publicationId);
    
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
        Chapter[] allChapters = this.GetChapters(manga, language);
        if (allChapters.Length < 1)
            return Array.Empty<Chapter>();
        
        Log($"Checking for duplicates {manga}");
        List<Chapter> newChaptersList = allChapters.Where(nChapter => float.TryParse(nChapter.chapterNumber, numberFormatDecimalPoint, out float chapterNumber)
                                                                      && chapterNumber > manga.ignoreChaptersBelow
                                                                      && !nChapter.CheckChapterIsDownloaded()).ToList();
        Log($"{newChaptersList.Count} new chapters. {manga}");
        try
        {
            Chapter latestChapterAvailable =
                allChapters.Max();
            manga.latestChapterAvailable =
                Convert.ToSingle(latestChapterAvailable.chapterNumber, numberFormatDecimalPoint);
        }
        catch (Exception e)
        {
            Log(e.ToString());
            Log($"Failed getting new Chapters for {manga}");
        }
        
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
    /// <param name="retries">Number of times to retry to copy the cover (or download it first)</param>
    public void CopyCoverFromCacheToDownloadLocation(Manga manga, int? retries = 1)
    {
        Log($"Copy cover {manga}");
        //Check if Publication already has a Folder and cover
        string publicationFolder = manga.CreatePublicationFolder(TrangaSettings.downloadLocation);
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            Log($"Cover exists {manga}");
            return;
        }

        string? fileInCache = manga.coverFileNameInCache;
        if (fileInCache is null || !File.Exists(fileInCache))
        {
            Log($"Cloning cover failed: File missing {fileInCache}.");
            if (retries > 0 && manga.coverUrl is not null)
            {
                Log($"Trying {retries} more times");
                SaveCoverImageToCache(manga.coverUrl, manga.internalId, 0);
                CopyCoverFromCacheToDownloadLocation(manga, --retries);
            }

            return;
        }
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
    private HttpStatusCode DownloadImage(string imageUrl, string fullPath, RequestType requestType, string? referrer = null)
    {
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, requestType, referrer);
        
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return requestResult.statusCode;
        if (requestResult.result == Stream.Null)
            return HttpStatusCode.NotFound;

        FileStream fs = new (fullPath, FileMode.Create);
        requestResult.result.CopyTo(fs);
        fs.Close();
        return requestResult.statusCode;
    }

    private void ProcessImage(string imagePath)
    {
        if (!TrangaSettings.bwImages && TrangaSettings.compression == 100)
            return;
        DateTime start = DateTime.Now;
        using Image image = Image.Load(imagePath);
        File.Delete(imagePath);
        if(TrangaSettings.bwImages) 
            image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
        image.SaveAsJpeg(imagePath, new JpegEncoder()
        {
            Quality = TrangaSettings.compression
        });
        Log($"Image processing took {DateTime.Now.Subtract(start):s\\.fff} B/W:{TrangaSettings.bwImages} Compression: {TrangaSettings.compression}");
    }

    protected HttpStatusCode DownloadChapterImages(string[] imageUrls, Chapter chapter, RequestType requestType, string? referrer = null, ProgressToken? progressToken = null)
    {
        string saveArchiveFilePath = chapter.GetArchiveFilePath();
        
        if (progressToken?.cancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Log($"Downloading Images for {saveArchiveFilePath}");
        if (progressToken is not null)
            progressToken.increments += imageUrls.Length;
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Directory.CreateDirectory(directoryPath,
                    UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );
            else
                Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice.
        {
            progressToken?.Complete();
            return HttpStatusCode.Created;
        }
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory("trangatemp").FullName;

        int chapterNum = 0;
        //Download all Images to temporary Folder
        if (imageUrls.Length == 0)
        {
            Log("No images found");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute);
            Directory.Delete(tempFolder, true);
            progressToken?.Complete();
            return HttpStatusCode.NoContent;
        }
        
        foreach (string imageUrl in imageUrls)
        {
            string extension = imageUrl.Split('.')[^1].Split('?')[0];
            Log($"Downloading image {chapterNum + 1:000}/{imageUrls.Length:000}");
            string imagePath = Path.Join(tempFolder, $"{chapterNum++}.{extension}");
            HttpStatusCode status = DownloadImage(imageUrl, imagePath, requestType, referrer);
            ProcessImage(imagePath);
            Log($"{saveArchiveFilePath} {chapterNum + 1:000}/{imageUrls.Length:000} {status}");
            if ((int)status < 200 || (int)status >= 300)
            {
                progressToken?.Complete();
                return status;
            }
            if (progressToken?.cancellationRequested ?? false)
            {
                progressToken.Complete();
                return HttpStatusCode.RequestTimeout;
            }
            progressToken?.Increment();
        }
        
        File.WriteAllText(Path.Join(tempFolder, "ComicInfo.xml"), chapter.GetComicInfoXmlString());
        
        Log($"Creating archive {saveArchiveFilePath}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        chapter.CreateChapterMarker();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        Log("Created archive.");
        progressToken?.Complete();
        Log("Download complete.");
        return HttpStatusCode.OK;
    }
    
    protected string SaveCoverImageToCache(string url, string mangaInternalId, RequestType requestType)
    {
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(url);
        string filename = $"{match.Groups[1].Value}-{mangaInternalId}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.coverImageCache, filename);

        if (File.Exists(saveImagePath))
            return saveImagePath;
        
        RequestResult coverResult = downloadClient.MakeRequest(url, requestType);
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        Directory.CreateDirectory(TrangaSettings.coverImageCache);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        Log($"Saving cover to {saveImagePath}");
        return saveImagePath;
    }
}