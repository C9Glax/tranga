using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using static System.IO.UnixFileMode;

namespace API.Schema;

[PrimaryKey("MangaId")]
public class Manga
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; }
    [StringLength(512)] [Required] public string Name { get; internal set; }
    [Required] public string Description { get; internal set; }
    [JsonIgnore] [Url] [StringLength(512)] public string CoverUrl { get; internal set; }
    [Required] public MangaReleaseStatus ReleaseStatus { get; internal set; }
    [StringLength(64)] public string? LibraryId { get; private set; }
    private LocalLibrary? _library = null!;
    [JsonIgnore]
    public LocalLibrary? Library
    {
        get => _lazyLoader.Load(this, ref _library);
        set
        {
            LibraryId = value?.LocalLibraryId;
            _library = value;
        }
    }

    public ICollection<Author> Authors { get; internal set; }= null!;
    public ICollection<MangaTag> MangaTags { get; internal set; }= null!;
    public ICollection<Link> Links { get; internal set; }= null!;
    public ICollection<MangaAltTitle> AltTitles { get; internal set; } = null!;
    [Required] public float IgnoreChaptersBefore { get; internal set; }
    [StringLength(1024)] [Required] public string DirectoryName { get; private set; }
    [JsonIgnore] [StringLength(512)] public string? CoverFileNameInCache { get; internal set; } = null;
    public uint? Year { get; internal init; }
    [StringLength(8)] public string? OriginalLanguage { get; internal init; }

    [JsonIgnore]
    [NotMapped]
    public string? FullDirectoryPath => Library is not null ? Path.Join(Library.BasePath, DirectoryName) : null;

    [NotMapped] public ICollection<string> ChapterIds => Chapters.Select(c => c.ChapterId).ToList();
    private ICollection<Chapter>? _chapters = null!;
    [JsonIgnore]
    public ICollection<Chapter> Chapters
    {
        get => _lazyLoader.Load(this, ref _chapters) ?? throw new InvalidOperationException();
        init => _chapters = value;
    }

    [NotMapped]
    public ICollection<string> LinkedMangaConnectors =>
        MangaConnectorLinkedToManga.Select(l => l.MangaConnectorName).ToList();
    private ICollection<MangaConnectorMangaEntry>? _mangaConnectorLinkedToManga = null!;
    [JsonIgnore]
    public ICollection<MangaConnectorMangaEntry> MangaConnectorLinkedToManga
    {
        get => _lazyLoader.Load(this, ref _mangaConnectorLinkedToManga) ?? throw new InvalidOperationException();
        init => _mangaConnectorLinkedToManga = value;
    }
    
    private readonly ILazyLoader _lazyLoader = null!;

    public Manga(string name, string description, string coverUrl, MangaReleaseStatus releaseStatus,
        ICollection<Author> authors, ICollection<MangaTag> mangaTags, ICollection<Link> links, ICollection<MangaAltTitle> altTitles,
        LocalLibrary? library = null, float ignoreChaptersBefore = 0f, uint? year = null, string? originalLanguage = null)
    {
        this.MangaId = TokenGen.CreateToken(typeof(Manga), name);
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
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    public Manga(ILazyLoader lazyLoader, string mangaId, string name, string description, string coverUrl, MangaReleaseStatus releaseStatus,
        string directoryName, float ignoreChaptersBefore, string? libraryId, uint? year, string? originalLanguage)
    {
        this._lazyLoader = lazyLoader;
        this.MangaId = mangaId;
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

    public override string ToString()
    {
        return $"{MangaId} {Name}";
    }
}