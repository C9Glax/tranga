using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("MangaId")]
public class Manga
{
    [MaxLength(64)]
    public string MangaId { get; init; } = TokenGen.CreateToken(typeof(Manga), 64);
    [MaxLength(64)]
    public string ConnectorId { get; init; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CoverFileNameInCache { get; set; }
    public uint year { get; set; }
    public string? OriginalLanguage { get; set; }
    public Tranga.Manga.ReleaseStatusByte ReleaseStatus { get; set; }
    public string FolderName { get; set; }
    public float IgnoreChapterBefore { get; set; }
    [ForeignKey("LatestChapterDownloaded")]
    public string? LatestChapterDownloadedId { get; set; }
    [JsonIgnore]
    internal Chapter? LatestChapterDownloaded { get; }
    [ForeignKey("LatestChapterAvailable")]
    public string? LatestChapterAvailableId { get; set; }
    [JsonIgnore]
    internal Chapter? LatestChapterAvailable { get; }
    [ForeignKey("MangaConnector")]
    public string MangaConnectorId { get; init; }
    [JsonIgnore]
    internal MangaConnector MangaConnector { get; }
    [ForeignKey("Authors")]
    public string[] AuthorIds { get; set; }
    [JsonIgnore]
    internal Author[] Authors { get; }
    [ForeignKey("Tags")]
    public string[] TagIds { get; set; }
    [JsonIgnore]
    internal MangaTag[] Tags { get; }
    [ForeignKey("Links")]
    public string[] LinkIds { get; set; }
    [JsonIgnore]
    internal Link[] Links { get; }
    [ForeignKey("AltTitles")]
    public string[] AltTitleIds { get; set; }
    [JsonIgnore]
    internal MangaAltTitle[] AltTitles { get; }
    
}