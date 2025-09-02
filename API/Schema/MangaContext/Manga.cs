using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;
using API.Workers;
using Microsoft.EntityFrameworkCore;
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

    [NotMapped] public string? FullDirectoryPath => Library is not null ? Path.Join(Library.BasePath, DirectoryName) : null;

    [NotMapped] public ICollection<string> ChapterIds => Chapters.Select(c => c.Key).ToList();
    public ICollection<Chapter> Chapters = null!;

    [NotMapped] public Dictionary<string, string> IdsOnMangaConnectors => MangaConnectorIds.ToDictionary(id => id.MangaConnectorName, id => id.IdOnConnectorSite);
    [NotMapped] public ICollection<string> MangaConnectorIdsIds => MangaConnectorIds.Select(id => id.Key).ToList();
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
        this.DirectoryName = CleanDirectoryName(name);
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
    
    
    public string CreatePublicationFolder()
    {
        string? publicationFolder = FullDirectoryPath;
        if (publicationFolder is null)
            throw new DirectoryNotFoundException("Publication folder not found");
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(publicationFolder, GroupRead | GroupWrite | GroupExecute | OtherRead | OtherWrite | OtherExecute | UserRead | UserWrite | UserExecute);
        return publicationFolder;
    }

    //https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file
    //less than 32 is control *forbidden*
    //34 is " *forbidden*
    //42 is * *forbidden*
    //47 is / *forbidden*
    //58 is : *forbidden*
    //60 is < *forbidden*
    //62 is > *forbidden*
    //63 is ? *forbidden*
    //92 is \ *forbidden*
    //124 is | *forbidden*
    //127 is delete *forbidden*
    //Below 127 all except *******
    private static readonly int[] ForbiddenCharsBelow127 = [34, 42, 47, 58, 60, 62, 63, 92, 124, 127];
    //Above 127 none except *******
    private static readonly int[] IncludeCharsAbove127 = [128, 138, 142];
    //128 is € include
    //138 is Š include
    //142 is Ž include
    //152 through 255 looks fine except 157, 172, 173, 175 *******
    private static readonly int[] ForbiddenCharsAbove152 = [157, 172, 173, 175];
    private static string CleanDirectoryName(string name)
    {
        StringBuilder sb = new ();
        foreach (char c in name)
        {
            if (c >= 32 && c < 127 && ForbiddenCharsBelow127.Contains(c) == false)
                sb.Append(c);
            else if (c > 127 && c < 152 && IncludeCharsAbove127.Contains(c))
                sb.Append(c);
            else if(c >= 152 && c <= 255 && ForbiddenCharsAbove152.Contains(c) == false)
                sb.Append(c);
        }
        return sb.ToString();
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
            string oldPath = otherChapter.FullArchiveFilePath;
            Chapter newChapter = new(this, otherChapter.ChapterNumber, otherChapter.VolumeNumber,
                otherChapter.Title);
            this.Chapters.Add(newChapter);
            string newPath = newChapter.FullArchiveFilePath;
            newJobs.Add(new MoveFileOrFolderWorker(newPath, oldPath));
        }
        
        return newJobs.ToArray();
    }

    public override string ToString() => $"{base.ToString()} {Name}";
}

public enum MangaReleaseStatus : byte
{
    Continuing = 0,
    Completed = 1,
    OnHiatus = 2,
    Cancelled = 3,
    Unreleased = 4
}