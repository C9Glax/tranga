using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace API.MangaConnectors;

[PrimaryKey("Name")]
public abstract class MangaConnector(string name, string[] supportedLanguages, string[] baseUris, string iconUrl)
{
    [NotMapped] internal IDownloadClient downloadClient { get; init; } = null!;
    [NotMapped] protected ILog Log { get; init; } = LogManager.GetLogger(name);
    [StringLength(32)] public string Name { get; init; } = name;
    [StringLength(8)] public string[] SupportedLanguages { get; init; } = supportedLanguages;
    [StringLength(2048)] public string IconUrl { get; init; } = iconUrl;
    [StringLength(256)] public string[] BaseUris { get; init; } = baseUris;
    public bool Enabled { get; internal set; } = true;
    
    public abstract (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName);

    public abstract (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url);

    public abstract (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite);
    
    public abstract (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaId,
        string? language = null);

    internal abstract string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId);

    public bool UrlMatchesConnector(string url) => BaseUris.Any(baseUri => Regex.IsMatch(url, "https?://" + baseUri + "/.*"));
    
    internal string? SaveCoverImageToCache(MangaConnectorId<Manga> mangaId, int retries = 3)
    {
        if(retries < 0)
            return null;
        
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(mangaId.Obj.CoverUrl);
        string filename = $"{match.Groups[1].Value}-{mangaId.ObjId}.{mangaId.MangaConnectorName}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.CoverImageCacheOriginal, filename);

        if (File.Exists(saveImagePath))
            return filename;
        
        HttpResponseMessage coverResult = downloadClient.MakeRequest(mangaId.Obj.CoverUrl, RequestType.MangaCover, $"https://{match.Groups[1].Value}").Result;
        if ((int)coverResult.StatusCode < 200 || (int)coverResult.StatusCode >= 300)
            return SaveCoverImageToCache(mangaId, retries - 1);
            
        try
        {
            using MemoryStream ms = new();
            coverResult.Content.ReadAsStream().CopyTo(ms);
            byte[] imageBytes = ms.ToArray();
            Directory.CreateDirectory(TrangaSettings.CoverImageCacheOriginal);
            File.WriteAllBytes(saveImagePath, imageBytes);

            using Image image = Image.Load(imageBytes);
            Directory.CreateDirectory(TrangaSettings.CoverImageCacheLarge);
            using Image large = image.Clone(x => x.Resize(new ResizeOptions
                { Size = Constants.ImageLgSize, Mode = ResizeMode.Max }));
            large.SaveAsJpeg(Path.Join(TrangaSettings.CoverImageCacheLarge, filename), new (){ Quality = 40 });
            
            Directory.CreateDirectory(TrangaSettings.CoverImageCacheMedium);
            using Image medium = image.Clone(x => x.Resize(new ResizeOptions
                { Size = Constants.ImageMdSize, Mode = ResizeMode.Max }));
            medium.SaveAsJpeg(Path.Join(TrangaSettings.CoverImageCacheMedium, filename), new (){ Quality = 40 });
            
            Directory.CreateDirectory(TrangaSettings.CoverImageCacheSmall);
            using Image small = image.Clone(x => x.Resize(new ResizeOptions
                { Size = Constants.ImageSmSize, Mode = ResizeMode.Max }));
            small.SaveAsJpeg(Path.Join(TrangaSettings.CoverImageCacheSmall, filename), new (){ Quality = 40 });
        }
        catch (Exception e)
        {
            Log.Error(e);
        }


        return filename.CleanNameForWindows();
    }
    
    public async Task<Stream?> DownloadImage(string imageUrl, CancellationToken ct)
    {
        HttpResponseMessage requestResult = await downloadClient.MakeRequest(imageUrl, RequestType.MangaImage, cancellationToken: ct);
        return requestResult.IsSuccessStatusCode ? await requestResult.Content.ReadAsStreamAsync(ct) : null;
    }
}