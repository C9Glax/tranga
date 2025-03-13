using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.MangaConnectors;

[PrimaryKey("Name")]
public abstract class MangaConnector(string name, string[] supportedLanguages, string[] baseUris, string iconUrl)
{
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
    
    public abstract (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "");

    public abstract (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url);

    public abstract (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId);
    
    public abstract Chapter[] GetChapters(Manga manga, string language="en");
    
    [JsonIgnore]
    [NotMapped]
    internal DownloadClient downloadClient { get; init; } = null!;
    
    public Chapter[] GetNewChapters(Manga manga)
    {
        Chapter[] allChapters = GetChapters(manga);
        if (allChapters.Length < 1)
            return [];
        
        return allChapters.Where(chapter => !chapter.IsDownloaded()).ToArray();
    }

    internal abstract string[] GetChapterImageUrls(Chapter chapter);

    public bool ValidateUrl(string url) => BaseUris.Any(baseUri => Regex.IsMatch(url, "https?://" + baseUri + "/.*"));
}