using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using API.Schema.Jobs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("ChapterId")]
public class Chapter : IComparable<Chapter>
{
    public Chapter(Manga parentManga, string url, string chapterNumber, int? volumeNumber = null, string? title = null)
        : this(parentManga.MangaId, url, chapterNumber, volumeNumber, title)
    {
        ParentManga = parentManga;
        ArchiveFileName = BuildArchiveFileName();
    }

    public Chapter(string parentMangaId, string url, string chapterNumber,
        int? volumeNumber = null, string? title = null)
    {
        ChapterId = TokenGen.CreateToken(typeof(Chapter), parentMangaId, (volumeNumber ?? 0).ToString(), chapterNumber);
        ParentMangaId = parentMangaId;
        Url = url;
        ChapterNumber = chapterNumber;
        VolumeNumber = volumeNumber;
        Title = title;
    }

    [MaxLength(64)] public string ChapterId { get; init; }

    public int? VolumeNumber { get; private set; }

    [MaxLength(10)] public string ChapterNumber { get; private set; }

    public string Url { get; internal set; }
    public string? Title { get; private set; }
    public string ArchiveFileName { get; private set; }
    public bool Downloaded { get; internal set; } = false;

    public string ParentMangaId { get; internal set; }
    [JsonIgnore] public Manga? ParentManga { get; init; }

    public int CompareTo(Chapter? other)
    {
        if (other is not { } otherChapter)
            throw new ArgumentException($"{other} can not be compared to {this}");
        return VolumeNumber?.CompareTo(otherChapter.VolumeNumber) switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => CompareChapterNumbers(ChapterNumber, otherChapter.ChapterNumber)
        };
    }

    public MoveFileOrFolderJob? UpdateChapterNumber(string chapterNumber)
    {
        ChapterNumber = chapterNumber;
        return UpdateArchiveFileName();
    }

    public MoveFileOrFolderJob? UpdateVolumeNumber(int? volumeNumber)
    {
        VolumeNumber = volumeNumber;
        return UpdateArchiveFileName();
    }

    public MoveFileOrFolderJob? UpdateTitle(string? title)
    {
        Title = title;
        return UpdateArchiveFileName();
    }

    private string BuildArchiveFileName()
    {
        return
            $"{ParentManga.Name} - Vol.{VolumeNumber ?? 0} Ch.{ChapterNumber}{(Title is null ? "" : $" - {Title}")}.cbz";
    }

    private MoveFileOrFolderJob? UpdateArchiveFileName()
    {
        string oldPath = GetArchiveFilePath();
        ArchiveFileName = BuildArchiveFileName();
        return Downloaded ? new MoveFileOrFolderJob(oldPath, GetArchiveFilePath()) : null;
    }

    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    internal string GetArchiveFilePath()
    {
        return Path.Join(TrangaSettings.downloadLocation, ParentManga.FolderName, ArchiveFileName);
    }

    /// <summary>
    /// Checks the filesystem if an archive at the ArchiveFilePath exists
    /// </summary>
    /// <returns>True if archive exists on disk</returns>
    public bool IsDownloaded()
    {
        string path = GetArchiveFilePath();
        return File.Exists(path);
    }

    private static int CompareChapterNumbers(string ch1, string ch2)
    {
        int[] ch1Arr = ch1.Split('.').Select(c => int.TryParse(c, out int result) ? result : -1).ToArray();
        int[] ch2Arr = ch2.Split('.').Select(c => int.TryParse(c, out int result) ? result : -1).ToArray();
        
        if (ch1Arr.Contains(-1) || ch2Arr.Contains(-1))
            throw new ArgumentException("Chapter number is not in correct format");
        
        int i = 0, j = 0;

        while (i < ch1Arr.Length && j < ch2Arr.Length)
        {
            if (ch1Arr[i] < ch2Arr[j])
                return -1;
            if (ch1Arr[i] > ch2Arr[j])
                return 1;
            i++;
            j++;
        }

        return 0;
    }

    internal string GetComicInfoXmlString()
    {
        XElement comicInfo = new("ComicInfo",
            new XElement("Tags", string.Join(',', ParentManga.MangaTags.Select(tag => tag.Tag))),
            new XElement("LanguageISO", ParentManga.OriginalLanguage),
            new XElement("Title", Title),
            new XElement("Writer", string.Join(',', ParentManga.Authors.Select(author => author.AuthorName))),
            new XElement("Volume", VolumeNumber),
            new XElement("Number", ChapterNumber)
        );
        return comicInfo.ToString();
    }
}