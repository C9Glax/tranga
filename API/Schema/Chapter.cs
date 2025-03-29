using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
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
        FileName = GetArchiveFilePath(parentManga.Name);
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

    [StringLength(64)]
    [Required]
    public string ChapterId { get; init; }
    public int? VolumeNumber { get; private set; }
    [StringLength(10)]
    [Required]
    public string ChapterNumber { get; private set; }

    [StringLength(2048)]
    [Required]
    [Url]
    public string Url { get; internal set; }
    [StringLength(256)]
    public string? Title { get; private set; }
    [StringLength(256)]
    [Required]
    public string FileName { get; private set; }
    [JsonIgnore]
    [NotMapped]
    public string? FullArchiveFilePath => ParentManga is { } m ? Path.Join(m.FullDirectoryPath, FileName) : null;
    [Required]
    public bool Downloaded { get; internal set; } = false;
    [Required]
    [StringLength(64)]
    public string ParentMangaId { get; internal set; }
    [JsonIgnore]
    public Manga? ParentManga { get; init; }

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

    internal MoveFileOrFolderJob? UpdateArchiveFileName()
    {
        string? oldPath = FullArchiveFilePath;
        if (oldPath is null)
            return null;
        string newPath = GetArchiveFilePath();
        FileName = newPath;
        return Downloaded ? new MoveFileOrFolderJob(oldPath, newPath) : null;
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

    /// Placeholders:
    /// %M Manga Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Manga Internal ID
    /// %Y Year (Manga)
    private static readonly Regex NullableRex = new(@"\?([a-zA-Z])\(([^\)]*)\)|(.+?)");
    private static readonly Regex ReplaceRexx = new(@"%([a-zA-Z])|(.+?)");
    private string GetArchiveFilePath(string? parentMangaName = null)
    {
        string archiveNamingScheme = TrangaSettings.chapterNamingScheme;
        StringBuilder stringBuilder = new();
        foreach (Match nullable in  NullableRex.Matches(archiveNamingScheme))
        {
            if (nullable.Groups[3].Success)
            {
                stringBuilder.Append(nullable.Groups[3].Value);
                continue;
            }

            char placeholder = nullable.Groups[1].Value[0];
            bool isNull = placeholder switch
            {
                'M' => ParentManga?.Name is null && parentMangaName is null,
                'V' => VolumeNumber is null,
                'C' => ChapterNumber is null,
                'T' => Title is null,
                'A' => ParentManga?.Authors?.FirstOrDefault()?.AuthorName is null,
                'I' => ChapterId is null,
                'i' => ParentMangaId is null,
                'Y' => ParentManga?.Year is null,
                _ => true
            };
            if(!isNull)
                stringBuilder.Append(nullable.Groups[2].Value);
        }
        
        string checkedString = stringBuilder.ToString();
        stringBuilder = new();
        
        foreach (Match replace in ReplaceRexx.Matches(checkedString))
        {
            if (replace.Groups[2].Success)
            {
                stringBuilder.Append(replace.Groups[2].Value);
                continue;
            }
            
            char placeholder = replace.Groups[1].Value[0];
            string? value = placeholder switch
            {
                'M' => ParentManga?.Name ?? parentMangaName,
                'V' => VolumeNumber?.ToString(),
                'C' => ChapterNumber,
                'T' => Title,
                'A' => ParentManga?.Authors?.FirstOrDefault()?.AuthorName,
                'I' => ChapterId,
                'i' => ParentMangaId,
                'Y' => ParentManga?.Year.ToString(),
                _ => null
            };
            stringBuilder.Append(value);
        }

        stringBuilder.Append(".cbz");

        return stringBuilder.ToString();
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