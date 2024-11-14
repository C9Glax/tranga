using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using API.Schema;
using log4net;
using log4net.Config;
using static System.IO.UnixFileMode;

namespace Tranga.MangaConnectors;

/// <summary>
/// Base-Class for all Connectors
/// Provides some methods to be used by all Connectors, as well as a DownloadClient
/// </summary>
public abstract class MangaConnector
{
    internal DownloadClient downloadClient { get; init; }
    protected readonly string MangaConnectorId;
    protected readonly ILog log;
    protected static readonly NumberFormatInfo NumberFormatDecimalPoint = new (){ NumberDecimalSeparator = "." };
    private readonly string? _referrer = null;

    protected MangaConnector(string mangaConnectorId, DownloadClient downloadClient, string? referrer = null)
    {
        this.MangaConnectorId = mangaConnectorId;
        this.downloadClient = downloadClient;
        log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
        this._referrer = referrer;
    }

    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "");

    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url);

    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId);
    public (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromManga(Manga manga) => GetMangaFromId(manga.MangaConnectorId);
    
    public abstract Chapter[] GetChapters(Manga manga, string language="en");
    
    protected abstract string[] GetChapterImages(Chapter chapter);

    public void CopyCoverFromCacheToDownloadLocation(Manga manga, string coverImageCachePath, string downloadLocation)
    {
        log.Info($"Copy cover {manga}");
        if (manga.CoverFileNameInCache is null)
        {
            log.Debug($"Cover not yet downloaded {manga}");
            return;
        }
        string readImagePath = Path.Join(coverImageCachePath, manga.CoverFileNameInCache);
        if (!File.Exists(readImagePath))
        {
            log.Error($"Cover does not exist at {readImagePath}! {manga}");
            return;
        }
        string saveImagePath = Path.Join(downloadLocation, $"cover.{Path.GetFileName(manga.CoverFileNameInCache).Split('.')[^1]}" );
        
        log.Info($"Cloning cover {readImagePath} -> {saveImagePath}");
        try
        {
            File.Copy(readImagePath, saveImagePath, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                File.SetUnixFileMode(saveImagePath, GroupRead | GroupWrite | UserRead | UserWrite);
        }
        catch (Exception e)
        {
            log.Error(e.Message);
        }
    }
    
    private bool DownloadImage(string imageUrl, string fullPath, string? referrer = null)
    {
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, RequestType.MangaImage, referrer);
        
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return false;
        if (requestResult.result == Stream.Null)
            return false;

        try
        {
            FileStream fs = new(fullPath, FileMode.Create);
            requestResult.result.CopyTo(fs);
            fs.Close();
            return true;
        }
        catch (Exception e)
        {
            log.Error(e.Message);
            return false;
        }
    }

    public bool DownloadChapterImages(Chapter chapter, out string? path)
    {
        path = null;
        log.Info($"Downloading Images for {chapter}");
        string[] imageUrls = GetChapterImages(chapter);
        if (imageUrls.Length < 1)
        {
            log.Info("No images found");
            return false;
        }
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory("TrangaTmp").FullName;

        for (int chapterImage = 1; chapterImage <= imageUrls.Length; chapterImage++)
        {
            string url = imageUrls[chapterImage];
            string extension = url.Split('.')[^1].Split('?')[0];
            log.Info($"Downloading image {chapterImage:000}/{imageUrls.Length:000}");
            string imagePath = Path.Join(tempFolder, $"{chapterImage}.{extension}");
            bool status = DownloadImage(url, imagePath, _referrer);
            if (status == false)
                return false;
        }
        path = tempFolder;
        return true;
    }
    
    protected string SaveCoverImageToCache(Manga manga, string coverImageCachePath)
    {
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(manga.CoverUrl);
        string filename = $"{match.Groups[1].Value}-{manga.MangaId}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(coverImageCachePath, filename);

        if (File.Exists(saveImagePath))
            return filename;
        
        RequestResult coverResult = downloadClient.MakeRequest(manga.CoverUrl, RequestType.MangaCover);
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        Directory.CreateDirectory(coverImageCachePath);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        log.Info($"Saving cover to {saveImagePath}");
        return filename;
    }
}