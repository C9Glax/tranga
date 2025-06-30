using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema;

[PrimaryKey("ChapterId")]
public class Chapter : IComparable<Chapter>
{
    [StringLength(64)] [Required] public string ChapterId { get; init; }

    [StringLength(256)]public string? IdOnConnectorSite { get; init; }

    [StringLength(64)] [Required] public string ParentMangaId { get; init; } = null!;
    private Manga? _parentManga = null!;

    [JsonIgnore]
    public Manga ParentManga
    {
        get => _lazyLoader.Load(this, ref _parentManga) ?? throw new InvalidOperationException();
        init
        {
            ParentMangaId = value.MangaId;
            _parentManga = value;
        }
    }

    private MangaConnectorMangaEntry? _mangaConnectorMangaEntry = null!;
    [JsonIgnore]
    public MangaConnectorMangaEntry MangaConnectorMangaEntry
    {
        get => _lazyLoader.Load(this, ref _mangaConnectorMangaEntry) ?? throw new InvalidOperationException();
        init => _mangaConnectorMangaEntry = value;
    }

    public int? VolumeNumber { get; private set; }
    [StringLength(10)] [Required] public string ChapterNumber { get; private set; }

    [StringLength(2048)] [Required] [Url] public string Url { get; internal set; }

    [StringLength(256)] public string? Title { get; private set; }

    [StringLength(256)] [Required] public string FileName { get; private set; }

    [Required] public bool Downloaded { get; internal set; }
    [NotMapped] public string FullArchiveFilePath => Path.Join(MangaConnectorMangaEntry.Manga.FullDirectoryPath, FileName);

    private readonly ILazyLoader _lazyLoader = null!;

    public Chapter(MangaConnectorMangaEntry mangaConnectorMangaEntry, string url, string chapterNumber, int? volumeNumber = null, string? idOnConnectorSite = null, string? title = null)
    {
        this.ChapterId = TokenGen.CreateToken(typeof(Chapter), mangaConnectorMangaEntry.MangaId, chapterNumber);
        this.MangaConnectorMangaEntry = mangaConnectorMangaEntry;
        this.IdOnConnectorSite = idOnConnectorSite;
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
    internal Chapter(ILazyLoader lazyLoader, string chapterId, int? volumeNumber, string chapterNumber, string url, string? idOnConnectorSite, string? title, string fileName, bool downloaded)
    {
        this._lazyLoader = lazyLoader;
        this.ChapterId = chapterId;
        this.IdOnConnectorSite = idOnConnectorSite;
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
                'M' => MangaConnectorMangaEntry.Manga?.Name is null,
                'V' => VolumeNumber is null,
                'C' => ChapterNumber is null,
                'T' => Title is null,
                'A' => MangaConnectorMangaEntry.Manga?.Authors?.FirstOrDefault()?.AuthorName is null,
                'I' => ChapterId is null,
                'i' => MangaConnectorMangaEntry.Manga?.MangaId is null,
                'Y' => MangaConnectorMangaEntry.Manga?.Year is null,
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
                'M' => MangaConnectorMangaEntry.Manga?.Name,
                'V' => VolumeNumber?.ToString(),
                'C' => ChapterNumber,
                'T' => Title,
                'A' => MangaConnectorMangaEntry.Manga?.Authors?.FirstOrDefault()?.AuthorName,
                'I' => ChapterId,
                'i' => MangaConnectorMangaEntry.Manga?.MangaId,
                'Y' => MangaConnectorMangaEntry.Manga?.Year.ToString(),
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
        if(MangaConnectorMangaEntry.Manga.MangaTags.Count > 0)
            comicInfo.Add(new XElement("Tags", string.Join(',', MangaConnectorMangaEntry.Manga.MangaTags.Select(tag => tag.Tag))));
        if(VolumeNumber is not null)
            comicInfo.Add(new XElement("Volume", VolumeNumber));
        if(MangaConnectorMangaEntry.Manga.Authors.Count > 0)
            comicInfo.Add(new XElement("Writer", string.Join(',', MangaConnectorMangaEntry.Manga.Authors.Select(author => author.AuthorName))));
        if(MangaConnectorMangaEntry.Manga.OriginalLanguage is not null)
            comicInfo.Add(new XElement("LanguageISO", MangaConnectorMangaEntry.Manga.OriginalLanguage));
        if(MangaConnectorMangaEntry.Manga.Description != string.Empty)
            comicInfo.Add(new XElement("Summary", MangaConnectorMangaEntry.Manga.Description));
        return comicInfo.ToString();
    }

    public override string ToString()
    {
        return $"{ChapterId} Vol.{VolumeNumber} Ch.{ChapterNumber} - {Title}";
    }
}