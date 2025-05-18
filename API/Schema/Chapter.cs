using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("ChapterId")]
public class Chapter : IComparable<Chapter>
{
    [StringLength(64)] [Required] public string ChapterId { get; init; }

    public string ParentMangaId { get; init; }
    [JsonIgnore] public Manga ParentManga { get; init; } = null!;

    public int? VolumeNumber { get; private set; }
    [StringLength(10)] [Required] public string ChapterNumber { get; private set; }

    [StringLength(2048)] [Required] [Url] public string Url { get; internal set; }

    [StringLength(256)] public string? Title { get; private set; }

    [StringLength(256)] [Required] public string FileName { get; private set; }

    [Required] public bool Downloaded { get; internal set; }
    [NotMapped] public string FullArchiveFilePath => Path.Join(ParentManga.FullDirectoryPath, FileName);

    public Chapter(Manga parentManga, string url, string chapterNumber, int? volumeNumber = null, string? title = null)
    {
        this.ChapterId = TokenGen.CreateToken(typeof(Chapter), parentManga.MangaId, chapterNumber);
        this.ParentMangaId = parentManga.MangaId;
        this.ParentManga = parentManga;
        this.VolumeNumber = volumeNumber;
        this.ChapterNumber = chapterNumber;
        this.Url = url;
        this.Title = title;
        this.FileName = GetArchiveFilePath();
        this.Downloaded = false;
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal Chapter(string chapterId, string parentMangaId, int? volumeNumber, string chapterNumber, string url, string? title, string fileName, bool downloaded)
    {
        this.ChapterId = chapterId;
        this.ParentMangaId = parentMangaId;
        this.VolumeNumber = volumeNumber;
        this.ChapterNumber = chapterNumber;
        this.Url = url;
        this.Title = title;
        this.FileName = fileName;
        this.Downloaded = downloaded;
    }

    public int CompareTo(Chapter? other)
    {
        if (other is not { } otherChapter)
            throw new ArgumentException($"{other} can not be compared to {this}");
        //Compare Ids for Collection-Comparisons
        if(other.ChapterId.Equals(this.ChapterId))
            return 0;
        return VolumeNumber?.CompareTo(otherChapter.VolumeNumber) switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => CompareChapterNumbers(ChapterNumber, otherChapter.ChapterNumber)
        };
    }

    /// <summary>
    /// Checks the filesystem if an archive at the ArchiveFilePath exists
    /// </summary>
    /// <returns>True if archive exists on disk</returns>
    public bool CheckDownloaded() => File.Exists(FullArchiveFilePath);

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
    private string GetArchiveFilePath()
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
                'M' => ParentManga?.Name is null,
                'V' => VolumeNumber is null,
                'C' => ChapterNumber is null,
                'T' => Title is null,
                'A' => ParentManga?.Authors?.FirstOrDefault()?.AuthorName is null,
                'I' => ChapterId is null,
                'i' => ParentManga?.MangaId is null,
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
                'M' => ParentManga?.Name,
                'V' => VolumeNumber?.ToString(),
                'C' => ChapterNumber,
                'T' => Title,
                'A' => ParentManga?.Authors?.FirstOrDefault()?.AuthorName,
                'I' => ChapterId,
                'i' => ParentManga?.MangaId,
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
            new XElement("Number", ChapterNumber)
        );
        if(Title is not null)
            comicInfo.Add(new XElement("Title", Title));
        if(ParentManga.MangaTags.Count > 0)
            comicInfo.Add(new XElement("Tags", string.Join(',', ParentManga.MangaTags.Select(tag => tag.Tag))));
        if(VolumeNumber is not null)
            comicInfo.Add(new XElement("Volume", VolumeNumber));
        if(ParentManga.Authors.Count > 0)
            comicInfo.Add(new XElement("Writer", string.Join(',', ParentManga.Authors.Select(author => author.AuthorName))));
        if(ParentManga.OriginalLanguage is not null)
            comicInfo.Add(new XElement("LanguageISO", ParentManga.OriginalLanguage));
        return comicInfo.ToString();
    }

    public override string ToString()
    {
        return $"{ChapterId} Vol.{VolumeNumber} Ch.{ChapterNumber} - {Title}";
    }
}