using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.MangaDownloadClients;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.MangaConnectors;

[PrimaryKey("Name")]
public abstract class MangaConnector(string name, string[] supportedLanguages, string[] baseUris)
{
    [MaxLength(32)]
    public string Name { get; init; } = name;
    public string[] SupportedLanguages { get; init; } = supportedLanguages;
    public string[] BaseUris { get; init; } = baseUris;

    [ForeignKey("MangaIds")]
    public virtual Manga[] Mangas { get; internal set; } = [];
    
    
    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "");

    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url);

    public abstract (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId);
    
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
}