using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore;
using static System.IO.UnixFileMode;

namespace API.Schema;

[PrimaryKey("MangaId")]
public class Manga
{
    [MaxLength(64)]
    public string MangaId { get; init; }
    [MaxLength(64)]
    public string ConnectorId { get; init; }

    public string Name { get; internal set; }
    public string Description { get; internal set; }
    public string WebsiteUrl { get; internal set; }
    public string CoverUrl { get; internal set; }
    public string? CoverFileNameInCache { get; internal set; }
    public uint Year { get; internal set; }
    public string? OriginalLanguage { get; internal set; }
    public MangaReleaseStatus ReleaseStatus { get; internal set; }
    public string FolderName { get; private set; }
    public float IgnoreChapterBefore { get; internal set; }

    public string MangaConnectorId { get; private set; }
    
    public MangaConnector? MangaConnector { get; private set; }
    
    public ICollection<Author>? Authors { get; internal set; }
    
    public ICollection<MangaTag>? Tags { get; internal set; }
    
    public ICollection<Link>? Links { get; internal set; }
    
    public ICollection<MangaAltTitle>? AltTitles { get; internal set; }

    public Manga(string connectorId, string name, string description, string websiteUrl, string coverUrl,
        string? coverFileNameInCache, uint year, string? originalLanguage, MangaReleaseStatus releaseStatus,
        float ignoreChapterBefore, MangaConnector mangaConnector, ICollection<Author> authors,
        ICollection<MangaTag> tags, ICollection<Link> links, ICollection<MangaAltTitle> altTitles)
        : this(connectorId, name, description, websiteUrl, coverUrl, coverFileNameInCache, year, originalLanguage,
            releaseStatus, ignoreChapterBefore, mangaConnector.Name)
    {
        this.Authors = authors;
        this.Tags = tags;
        this.Links = links;
        this.AltTitles = altTitles;
    }
    
    public Manga(string connectorId, string name, string description, string websiteUrl, string coverUrl,
        string? coverFileNameInCache, uint year, string? originalLanguage, MangaReleaseStatus releaseStatus,
        float ignoreChapterBefore, string mangaConnectorId)
    {
        MangaId = TokenGen.CreateToken(typeof(Manga), mangaConnectorId, connectorId);
        ConnectorId = connectorId;
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
        FolderName = BuildFolderName(name);
    }

    public MoveFileOrFolderJob UpdateFolderName(string downloadLocation, string newName)
    {
        string oldName = this.FolderName;
        this.FolderName = newName;
        return new MoveFileOrFolderJob(Path.Join(downloadLocation, oldName), Path.Join(downloadLocation, this.FolderName));
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
        this.Tags = other.Tags;
        this.AltTitles = other.AltTitles;
        this.ReleaseStatus = other.ReleaseStatus;
    }

    private static string BuildFolderName(string mangaName)
    {
        return mangaName;
    }
    
    internal string SaveCoverImageToCache()
    {
        Regex urlRex = new (@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        //https?:\/\/[a-zA-Z0-9-]+\.([a-zA-Z0-9-]+\.[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+)) for only second level domains
        Match match = urlRex.Match(CoverUrl);
        string filename = $"{match.Groups[1].Value}-{MangaId}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.coverImageCache, filename);

        if (File.Exists(saveImagePath))
            return saveImagePath;
        
        RequestResult coverResult = new HttpDownloadClient().MakeRequest(CoverUrl, RequestType.MangaCover);
        using MemoryStream ms = new();
        coverResult.result.CopyTo(ms);
        Directory.CreateDirectory(TrangaSettings.coverImageCache);
        File.WriteAllBytes(saveImagePath, ms.ToArray());
        return saveImagePath;
    }
    
    public string CreatePublicationFolder()
    {
        string publicationFolder = Path.Join(TrangaSettings.downloadLocation, this.FolderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(publicationFolder, GroupRead | GroupWrite | GroupExecute | OtherRead | OtherWrite | OtherExecute | UserRead | UserWrite | UserExecute);
        return publicationFolder;
    }
    
    //TODO onchanges create job to update metadata files in archives, etc.
}