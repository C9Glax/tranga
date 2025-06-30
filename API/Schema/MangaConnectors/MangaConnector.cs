using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.MangaConnectors;

[PrimaryKey("Name")]
public abstract class MangaConnector(string name, string[] supportedLanguages, string[] baseUris, string iconUrl)
{
    [JsonIgnore]
    [NotMapped]
    internal DownloadClient downloadClient { get; init; } = null!;

    [JsonIgnore]
    [NotMapped]
    protected ILog Log { get; init; } = LogManager.GetLogger(name);
    
    [StringLength(32)]
    [Required]
    public string Name { get; init; } = name;
    [StringLength(8)]
    [Required]
    public string[] SupportedLanguages { get; init; } = supportedLanguages;
    [StringLength(2048)]
    [Required]
    public string IconUrl { get; init; } = iconUrl;
    [StringLength(256)]
    [Required]
    public string[] BaseUris { get; init; } = baseUris;
    [Required]
    public bool Enabled { get; internal set; } = true;
    
    public abstract MangaConnectorMangaEntry[] SearchManga(string mangaSearchName);

    public abstract MangaConnectorMangaEntry? GetMangaFromUrl(string url);

    public abstract MangaConnectorMangaEntry? GetMangaFromId(string mangaIdOnSite);
    
    public abstract Chapter[] GetChapters(MangaConnectorMangaEntry mangaConnectorMangaEntry, string? language = null);

    internal abstract string[] GetChapterImageUrls(Chapter chapter);

    public bool UrlMatchesConnector(string url) => BaseUris.Any(baseUri => Regex.IsMatch(url, "https?://" + baseUri + "/.*"));
    
    internal string? SaveCoverImageToCache(Manga manga, int retries = 3)
    {
        if(retries < 0)
            return null;
        
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(manga.CoverUrl);
        string filename = $"{match.Groups[1].Value}-{manga.MangaId}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.coverImageCache, filename);

        if (File.Exists(saveImagePath))
            return saveImagePath;
        
        RequestResult coverResult = downloadClient.MakeRequest(manga.CoverUrl, RequestType.MangaCover, $"https://{match.Groups[1].Value}");
        if ((int)coverResult.statusCode < 200 || (int)coverResult.statusCode >= 300)
            return SaveCoverImageToCache(manga, --retries);
            
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        Directory.CreateDirectory(TrangaSettings.coverImageCache);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        
        return saveImagePath;
    }
}