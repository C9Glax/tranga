using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Schema.Jobs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
    public string FolderName { get; private set; } = folderName; //TODO
    public float IgnoreChapterBefore { get; internal set; } = ignoreChapterBefore;

    [ForeignKey("LatestChapterDownloaded")]
    public string? LatestChapterDownloadedId { get; internal set; } = latestChapterDownloadedId;

    [JsonIgnore]
    public Chapter? LatestChapterDownloaded { get; }
    [ForeignKey("LatestChapterAvailable")]
    public string? LatestChapterAvailableId { get; internal set; } = latestChapterAvailableId;

    [JsonIgnore]
    public Chapter? LatestChapterAvailable { get; } = latestChapterAvailable;

    [ForeignKey("MangaConnector")]
    public string MangaConnectorId { get; init; } = mangaConnectorId;

    [JsonIgnore]
    public MangaConnector MangaConnector { get; }
    [ForeignKey("Authors")]
    public string[] AuthorIds { get; internal set; } = authorIds;

    [JsonIgnore]
    public Author[] Authors { get; }
    [ForeignKey("Tags")]
    public string[] TagIds { get; internal set; } = tagIds;

    [JsonIgnore]
    public MangaTag[] Tags { get; }
    [ForeignKey("Links")]
    public string[] LinkIds { get; internal set; } = linkIds;

    [JsonIgnore]
    public Link[] Links { get; }
    [ForeignKey("AltTitles")]
    public string[] AltTitleIds { get; internal set; } = altTitleIds;

    [JsonIgnore]
    public MangaAltTitle[] AltTitles { get; }

    public MoveFileJob UpdateFolderName(string downloadLocation, string newName)
    {
        string oldName = this.FolderName;
        this.FolderName = newName;
        return new MoveFileJob(Path.Join(downloadLocation, oldName), Path.Join(downloadLocation, this.FolderName));
    }

    public void UpdateWithInfo(Manga other)
    {
        //TODO
    }
    
    //TODO onchanges create job to update metadata files in archives, etc.
}