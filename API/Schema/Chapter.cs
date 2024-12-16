using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using API.Schema.Jobs;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("ChapterId")]
public class Chapter : IComparable<Chapter>
{
    [MaxLength(64)]
    public string ChapterId { get; init; } = TokenGen.CreateToken(typeof(Chapter), 64);
    public float? VolumeNumber { get; private set; }
    public float ChapterNumber { get; private set; }
    public string Url { get; internal set; }
    public string? Title { get; private set; }
    public string ArchiveFileName { get; private set; }
    public bool Downloaded { get; internal set; } = false;
    
    [ForeignKey("MangaId")]
    public Manga ParentManga { get; init; }

    public Chapter(Manga parentManga, string url, float chapterNumber,
        float? volumeNumber = null, string? title = null)
    {
        this.ParentManga = parentManga;
        this.Url = url;
        this.ChapterNumber = chapterNumber;
        this.VolumeNumber = volumeNumber;
        this.Title = title;
        this.ArchiveFileName = BuildArchiveFileName();
    }

    public Chapter(string url, float chapterNumber, float? volumeNumber = null, string? title = null)
        : this(null, url, chapterNumber, volumeNumber, title){}

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
        string oldPath = GetArchiveFilePath();
        this.ArchiveFileName = BuildArchiveFileName();
        if (Downloaded)
        {
            return new MoveFileOrFolderJob(oldPath, GetArchiveFilePath());
        }
        return null;
    }
    
    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    internal string GetArchiveFilePath()
    {
        return Path.Join(TrangaSettings.downloadLocation, ParentManga.FolderName, ArchiveFileName);
    }

    public bool IsDownloaded()
    {
        string path = GetArchiveFilePath();
        return File.Exists(path);
    }

    public int CompareTo(Chapter? other)
    {
        if(other is not { } otherChapter)
            throw new ArgumentException($"{other} can not be compared to {this}");
        return this.VolumeNumber?.CompareTo(otherChapter.VolumeNumber) switch
        {
            <0 => -1,
            >0 => 1,
            _ => this.ChapterNumber.CompareTo(otherChapter.ChapterNumber)
        };
    }
    
    internal string GetComicInfoXmlString()
    {
        XElement comicInfo = new XElement("ComicInfo",
            new XElement("Tags", string.Join(',', ParentManga.Tags.Select(tag => tag.Tag))),
            new XElement("LanguageISO", ParentManga.OriginalLanguage),
            new XElement("Title", this.Title),
            new XElement("Writer", string.Join(',', ParentManga.Authors.Select(author => author.AuthorName))),
            new XElement("Volume", this.VolumeNumber),
            new XElement("Number", this.ChapterNumber)
        );
        return comicInfo.ToString();
    }
}