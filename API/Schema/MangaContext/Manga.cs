using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using API.Workers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static System.IO.UnixFileMode;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class Manga : Identifiable
{
    [StringLength(512)] public string Name { get; internal set; }
    [Required] public string Description { get; internal set; }
    [Url] [StringLength(512)] public string CoverUrl { get; internal set; }
    public MangaReleaseStatus ReleaseStatus { get; internal set; }
    [StringLength(64)] public string? LibraryId { get; private set; }
    public FileLibrary? Library = null!;
    public ICollection<Author> Authors { get; internal set; } = null!;
    public ICollection<MangaTag> MangaTags { get; internal set; } = null!;
    public ICollection<Link> Links { get; internal set; } = null!;
    public ICollection<AltTitle> AltTitles { get; internal set; } = null!;
    public float IgnoreChaptersBefore { get; internal set; }
    [StringLength(1024)] [Required] public string DirectoryName { get; private set; }
    [StringLength(512)] public string? CoverFileNameInCache { get; internal set; }
    public uint? Year { get; internal init; }
    [StringLength(8)] public string? OriginalLanguage { get; internal init; }
    
    
    /// <exception cref="DirectoryNotFoundException">Library not loaded</exception>
    [NotMapped]
    [JsonIgnore]
    public string FullDirectoryPath => EnsureDirectoryExists();

    [NotMapped]
    public ICollection<string> ChapterIds => Chapters.Select(c => c.Key).ToList();
    [JsonIgnore]
    public ICollection<Chapter> Chapters = null!;

    [NotMapped]
    public Dictionary<string, string> IdsOnMangaConnectors => MangaConnectorIds.ToDictionary(id => id.MangaConnectorName, id => id.IdOnConnectorSite);
    [NotMapped]
    public ICollection<string> MangaConnectorIdsIds => MangaConnectorIds.Select(id => id.Key).ToList();
    [JsonIgnore]
    public ICollection<MangaConnectorId<Manga>> MangaConnectorIds = null!;

    public Manga(string name, string description, string coverUrl, MangaReleaseStatus releaseStatus,
        ICollection<Author> authors, ICollection<MangaTag> mangaTags, ICollection<Link> links, ICollection<AltTitle> altTitles,
        FileLibrary? library = null, float ignoreChaptersBefore = 0f, uint? year = null, string? originalLanguage = null)
    :base(TokenGen.CreateToken(typeof(Manga), name))
    {
        this.Name = name;
        this.Description = description;
        this.CoverUrl = coverUrl;
        this.ReleaseStatus = releaseStatus;
        this.Library = library;
        this.Authors = authors;
        this.MangaTags = mangaTags;
        this.Links = links;
        this.AltTitles = altTitles;
        this.IgnoreChaptersBefore = ignoreChaptersBefore;
        this.DirectoryName = name.CleanNameForWindows();
        this.Year = year;
        this.OriginalLanguage = originalLanguage;
        this.Chapters = [];
        this.MangaConnectorIds = [];
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    public Manga(string key, string name, string description, string coverUrl,
        MangaReleaseStatus releaseStatus,
        string directoryName, float ignoreChaptersBefore, string? libraryId, uint? year, string? originalLanguage)
        : base(key)
    {
        this.Name = name;
        this.Description = description;
        this.CoverUrl = coverUrl;
        this.ReleaseStatus = releaseStatus;
        this.DirectoryName = directoryName;
        this.LibraryId = libraryId;
        this.IgnoreChaptersBefore = ignoreChaptersBefore;
        this.Year = year;
        this.OriginalLanguage = originalLanguage;
    }
    
    /// <exception cref="DirectoryNotFoundException">Library not loaded</exception>
    private string EnsureDirectoryExists()
    {
        string? publicationFolder = Library is not null ? Path.Join(Library.BasePath, DirectoryName) : null;
        if (publicationFolder is null)
            throw new DirectoryNotFoundException("Publication folder not found");
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        return publicationFolder;
    }

    /// <summary>
    /// Merges another Manga (MangaConnectorIds and Chapters)
    /// </summary>
    /// <param name="other">The other <see cref="Manga" /> to merge</param>
    /// <param name="context"><see cref="MangaContext"/> to use for Database operations</param>
    /// <returns>An array of <see cref="MoveFileOrFolderWorker"/> for moving <see cref="Chapter"/> to new Directory</returns>
    public BaseWorker[] MergeFrom(Manga other, MangaContext context)
    {
        context.Mangas.Remove(other);
        List<BaseWorker> newJobs = new();

        this.MangaConnectorIds = this.MangaConnectorIds
            .UnionBy(other.MangaConnectorIds, id => id.MangaConnectorName)
            .ToList();

        foreach (Chapter otherChapter in other.Chapters)
        {
            if (otherChapter.FullArchiveFilePath is not { } oldPath)
                continue;
            Chapter newChapter = new(this, otherChapter.ChapterNumber, otherChapter.VolumeNumber,
                otherChapter.Title);
            this.Chapters.Add(newChapter);
            if (newChapter.FullArchiveFilePath is not { } newPath)
                continue;
            newJobs.Add(new MoveFileOrFolderWorker(newPath, oldPath));
        }
        
        return newJobs.ToArray();
    }

    public async Task<(MemoryStream stream, FileInfo fileInfo)?> GetCoverImage(string cachePath, CancellationToken ct)
    {
        string fullPath = Path.Join(cachePath, CoverFileNameInCache);
        if (!File.Exists(fullPath))
            return null;

        FileInfo fileInfo = new(fullPath);
        MemoryStream stream = new (await File.ReadAllBytesAsync(fullPath, ct));
        
        return (stream, fileInfo);
    }

    public override string ToString() => $"{base.ToString()} {Name}";
}

public enum MangaReleaseStatus
{
    Continuing,
    Completed,
    OnHiatus,
    Cancelled,
    Unreleased
}