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
public class Manga(
    string connectorId,
    string name,
    string description,
    string websiteUrl,
    string coverUrl,
    string? coverFileNameInCache,
    uint year,
    string? originalLanguage,
    MangaReleaseStatus releaseStatus,
    float ignoreChapterBefore,
    string? latestChapterDownloadedId,
    string? latestChapterAvailableId,
    string mangaConnectorName,
    string[] authorIds,
    string[] tagIds,
    string[] linkIds,
    string[] altTitleIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = TokenGen.CreateToken(typeof(Manga), 64);
    [MaxLength(64)]
    public string ConnectorId { get; init; } = connectorId;

    public string Name { get; internal set; } = name;
    public string Description { get; internal set; } = description;
    public string WebsiteUrl { get; internal set; } = websiteUrl;
    public string CoverUrl { get; internal set; } = coverUrl;
    public string? CoverFileNameInCache { get; internal set; } = coverFileNameInCache;
    public uint year { get; internal set; } = year;
    public string? OriginalLanguage { get; internal set; } = originalLanguage;
    public MangaReleaseStatus ReleaseStatus { get; internal set; } = releaseStatus;
    public string FolderName { get; private set; } = BuildFolderName(name);
    public float IgnoreChapterBefore { get; internal set; } = ignoreChapterBefore;

    public string? LatestChapterDownloadedId { get; internal set; } = latestChapterDownloadedId;
    public virtual Chapter? LatestChapterDownloaded { get; }
    
    public string? LatestChapterAvailableId { get; internal set; } = latestChapterAvailableId;
    public virtual Chapter? LatestChapterAvailable { get; }

    public string MangaConnectorName { get; init; } = mangaConnectorName;
    public virtual MangaConnector MangaConnector { get; }
    
    public string[] AuthorIds { get; internal set; } = authorIds;
    [ForeignKey("AuthorIds")]
    public virtual Author[] Authors { get; }
    
    public string[] TagIds { get; internal set; } = tagIds;
    [ForeignKey("TagIds")]
    public virtual MangaTag[] Tags { get; }
    
    public string[] LinkIds { get; internal set; } = linkIds;
    [ForeignKey("LinkIds")]
    public virtual Link[] Links { get; }
    
    public string[] AltTitleIds { get; internal set; } = altTitleIds;
    [ForeignKey("AltTitleIds")]
    public virtual MangaAltTitle[] AltTitles { get; }

    public MoveFileOrFolderJob UpdateFolderName(string downloadLocation, string newName)
    {
        string oldName = this.FolderName;
        this.FolderName = newName;
        return new MoveFileOrFolderJob(Path.Join(downloadLocation, oldName), Path.Join(downloadLocation, this.FolderName));
    }

    internal void UpdateWithInfo(Manga other)
    {
        this.Name = other.Name;
        this.year = other.year;
        this.Description = other.Description;
        this.CoverUrl = other.CoverUrl;
        this.OriginalLanguage = other.OriginalLanguage;
        this.AuthorIds = other.AuthorIds;
        this.LinkIds = other.LinkIds;
        this.TagIds = other.TagIds;
        this.AltTitleIds = other.AltTitleIds;
        this.LatestChapterAvailableId = other.LatestChapterAvailableId;
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