using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Schema.Jobs;
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
    [JsonIgnore]public Manga ParentManga { get; }
    public float? VolumeNumber { get; private set; }
    public float ChapterNumber { get; private set; }
    public string Url { get; internal set; }
    public string? Title { get; private set; }
    public string ArchiveFileName { get; private set; }
    public bool Downloaded { get; internal set; } = false;

    public Chapter(string parentMangaId, string url, float chapterNumber,
        float? volumeNumber = null, string? title = null)
    {
        this.ParentMangaId = parentMangaId;
        this.Url = url;
        this.ChapterNumber = chapterNumber;
        this.VolumeNumber = volumeNumber;
        this.Title = title;
        this.ArchiveFileName = BuildArchiveFileName();
    }

    public MoveFileOrFolderJob? UpdateChapterNumber(float chapterNumber)
    {
        this.ChapterNumber = chapterNumber;
        return UpdateArchiveFileName();
    }

    public MoveFileOrFolderJob? UpdateVolumeNumber(float? volumeNumber)
    {
        this.VolumeNumber = volumeNumber;
        return UpdateArchiveFileName();
    }

    public MoveFileOrFolderJob? UpdateTitle(string? title)
    {
        this.Title = title;
        return UpdateArchiveFileName();
    }

    private string BuildArchiveFileName()
    {
        return $"{this.ParentManga.Name} - Vol.{this.VolumeNumber ?? 0} Ch.{this.ChapterNumber}{(this.Title is null ? "" : $" - {this.Title}")}.cbz";
    }

    private MoveFileOrFolderJob? UpdateArchiveFileName()
    {
        string oldPath = GetArchiveFilePath(""); //TODO GET PATH
        this.ArchiveFileName = BuildArchiveFileName();
        if (Downloaded)
        {
            return new MoveFileOrFolderJob(oldPath, GetArchiveFilePath("")); //TODO GET PATH
        }
        return null;
    }

    public Chapter(Manga parentManga, string url, float chapterNumber,
        float? volumeNumber = null, string? title = null) : this(parentManga.MangaId, url, chapterNumber, volumeNumber, title)
    {
        this.ParentManga = parentManga;
    }
    
    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    internal string GetArchiveFilePath(string downloadLocation)
    {
        return Path.Join(downloadLocation, ParentManga.FolderName, ArchiveFileName);
    }
}