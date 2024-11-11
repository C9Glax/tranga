using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("ChapterId")]
public class Chapter
{
    [MaxLength(64)]
    public string ChapterId { get; init; } = TokenGen.CreateToken(typeof(Chapter), 64);
    [ForeignKey("ParentManga")]
    public string ParentMangaId { get; init; }
    [JsonIgnore]internal Manga ParentManga { get; }
    public float? VolumeNumber { get; set; }
    public float ChapterNumber { get; set; }
    public string Url { get; set; }
    public string ArchiveFileName { get; set; }

    public Chapter(string parentMangaId, string url, string archiveFileName, float chapterNumber,
        float? volumeNumber = null)
    {
        this.ParentMangaId = parentMangaId;
        this.Url = url;
        this.ArchiveFileName = archiveFileName;
        this.ChapterNumber = chapterNumber;
        this.VolumeNumber = volumeNumber;
    }

    public Chapter(Manga parentManga, string url, string archiveFileName, float chapterNumber,
        float? volumeNumber = null) : this(parentManga.MangaId, url, archiveFileName, chapterNumber, volumeNumber)
    {
        this.ParentManga = parentManga;
    }
}