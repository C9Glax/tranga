using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.IO.UnixFileMode;

namespace API.Schema;

[PrimaryKey("MangaId")]
public class Manga
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; }
    [StringLength(128)]
    [Required]
    public string IdOnConnectorSite { get; init; }
    [StringLength(256)]
    [Required]
    public string Name { get; internal set; }
    [Required]
    public string Description { get; internal set; }
    [Url]
    [StringLength(256)]
    [Required]
    public string WebsiteUrl { get; internal set; }
    [JsonIgnore]
    [Url]
    public string CoverUrl { get; internal set; }
    [JsonIgnore]
    public string? CoverFileNameInCache { get; internal set; }
    [Required]
    public uint Year { get; internal set; }
    [StringLength(8)]
    public string? OriginalLanguage { get; internal set; }
    [Required]
    public MangaReleaseStatus ReleaseStatus { get; internal set; }
    [StringLength(256)]
    [Required]
    public string DirectoryName { get; private set; }
    public LocalLibrary? Library { get; internal set; }
    [JsonIgnore]
    [NotMapped]
    public string LibraryPath => Library is null ? TrangaSettings.downloadLocation : Library.BasePath;
    [JsonIgnore]
    [NotMapped]
    public string FullDirectoryPath => Path.Join(LibraryPath, DirectoryName);
    [Required]
    public float IgnoreChapterBefore { get; internal set; }
    [StringLength(64)]
    [Required]
    public string MangaConnectorId { get; private set; }
    [JsonIgnore] public MangaConnector? MangaConnector { get; private set; }
    
    [JsonIgnore] public ICollection<Author>? Authors { get; internal set; }
    [NotMapped]
    [StringLength(64)]
    [Required]
    public IEnumerable<string> AuthorIds => Authors?.Select(a => a.AuthorId) ?? [];
    
    [JsonIgnore] public ICollection<MangaTag>? MangaTags { get; internal set; }
    [NotMapped]
    [StringLength(64)]
    [Required]
    public IEnumerable<string> Tags => MangaTags?.Select(t => t.Tag) ?? [];
    
    
    [JsonIgnore] public ICollection<Link>? Links { get; internal set; }
    [NotMapped]
    [StringLength(64)]
    [Required]
    public IEnumerable<string> LinkIds => Links?.Select(l => l.LinkId) ?? [];
    
    [JsonIgnore] public ICollection<MangaAltTitle>? AltTitles { get; internal set; }
    [NotMapped]
    [StringLength(64)]
    [Required]
    public IEnumerable<string> AltTitleIds => AltTitles?.Select(a => a.AltTitleId) ?? [];

    public Manga(string idOnConnectorSite, string name, string description, string websiteUrl, string coverUrl,
        string? coverFileNameInCache, uint year, string? originalLanguage, MangaReleaseStatus releaseStatus,
        float ignoreChapterBefore, MangaConnector mangaConnector, ICollection<Author> authors,
        ICollection<MangaTag> mangaTags, ICollection<Link> links, ICollection<MangaAltTitle> altTitles,
        LocalLibrary? library = null)
        : this(idOnConnectorSite, name, description, websiteUrl, coverUrl, coverFileNameInCache, year, originalLanguage,
            releaseStatus, ignoreChapterBefore, mangaConnector.Name)
    {
        this.Authors = authors;
        this.MangaTags = mangaTags;
        this.Links = links;
        this.AltTitles = altTitles;
        this.Library = library;
    }
    
    public Manga(string idOnConnectorSite, string name, string description, string websiteUrl, string coverUrl,
        string? coverFileNameInCache, uint year, string? originalLanguage, MangaReleaseStatus releaseStatus,
        float ignoreChapterBefore, string mangaConnectorId)
    {
        MangaId = TokenGen.CreateToken(typeof(Manga), mangaConnectorId, idOnConnectorSite);
        IdOnConnectorSite = idOnConnectorSite;
        Name = name;
        Description = description;
        WebsiteUrl = websiteUrl;
        CoverUrl = coverUrl;
        CoverFileNameInCache = coverFileNameInCache;
        Year = year;
        OriginalLanguage = originalLanguage;
        ReleaseStatus = releaseStatus;
        IgnoreChapterBefore = ignoreChapterBefore;
        MangaConnectorId = mangaConnectorId;
        DirectoryName = BuildFolderName(name);
    }

    public MoveFileOrFolderJob UpdateFolderName(string downloadLocation, string newName)
    {
        string oldName = this.DirectoryName;
        this.DirectoryName = newName;
        return new MoveFileOrFolderJob(Path.Join(downloadLocation, oldName), Path.Join(downloadLocation, this.DirectoryName));
    }

    internal void UpdateWithInfo(Manga other)
    {
        this.Name = other.Name;
        this.Year = other.Year;
        this.Description = other.Description;
        this.CoverUrl = other.CoverUrl;
        this.OriginalLanguage = other.OriginalLanguage;
        this.Authors = other.Authors;
        this.Links = other.Links;
        this.MangaTags = other.MangaTags;
        this.AltTitles = other.AltTitles;
        this.ReleaseStatus = other.ReleaseStatus;
    }

    private static string BuildFolderName(string mangaName)
    {
        return mangaName;
    }
    
    internal string? SaveCoverImageToCache(int retries = 3)
    {
        if(retries < 0)
            return null;
        
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(CoverUrl);
        string filename = $"{match.Groups[1].Value}-{MangaId}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.coverImageCache, filename);

        if (File.Exists(saveImagePath))
            return saveImagePath;
        
        RequestResult coverResult = new HttpDownloadClient().MakeRequest(CoverUrl, RequestType.MangaCover, $"https://{match.Groups[1].Value}");
        if (coverResult.statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous)
            return SaveCoverImageToCache(--retries);
            
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        Directory.CreateDirectory(TrangaSettings.coverImageCache);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        
        return saveImagePath;
    }
    
    public string CreatePublicationFolder()
    {
        string publicationFolder = Path.Join(LibraryPath, this.DirectoryName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(publicationFolder, GroupRead | GroupWrite | GroupExecute | OtherRead | OtherWrite | OtherExecute | UserRead | UserWrite | UserExecute);
        return publicationFolder;
    }
    
    //TODO onchanges create job to update metadata files in archives, etc.
}