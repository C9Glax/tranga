using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Schema.Jobs;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("MangaId")]
public class Manga(
    string connectorId,
    string name,
    string description,
    string coverUrl,
    string? coverFileNameInCache,
    uint year,
    string? originalLanguage,
    MangaReleaseStatus releaseStatus,
    float ignoreChapterBefore,
    string? latestChapterDownloadedId,
    string? latestChapterAvailableId,
    string mangaConnectorId,
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

    public string MangaConnectorId { get; init; } = mangaConnectorId;
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
    
    [ForeignKey("ChapterIds")]
    public virtual Chapter[] Chapters { get; internal set; }

    public MoveFileOrFolderJob UpdateFolderName(string downloadLocation, string newName)
    {
        string oldName = this.FolderName;
        this.FolderName = newName;
        return new MoveFileOrFolderJob(Path.Join(downloadLocation, oldName), Path.Join(downloadLocation, this.FolderName));
    }

    internal void UpdateWithInfo(Manga other)
    {
        //TODO
    }

    private static string BuildFolderName(string mangaName)
    {
        return mangaName;
    }
    
    //TODO onchanges create job to update metadata files in archives, etc.
}