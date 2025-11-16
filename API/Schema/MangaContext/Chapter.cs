using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Soenneker.Utils.String.NeedlemanWunsch;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class Chapter : Identifiable, IComparable<Chapter>
{
    [StringLength(64)] public string ParentMangaId { get; init; } = null!;
    public Manga ParentManga = null!;

    [NotMapped] public Dictionary<string, string> IdsOnMangaConnectors =>
        MangaConnectorIds.ToDictionary(id => id.MangaConnectorName, id => id.IdOnConnectorSite);
    public ICollection<MangaConnectorId<Chapter>> MangaConnectorIds = null!;

    public int? VolumeNumber { get; private set; }
    [StringLength(10)] public string ChapterNumber { get; private set; }

    [StringLength(256)] public string? Title { get; private set; }

    [StringLength(256)] public string? FileName { get; internal set; }

    public bool Downloaded { get; internal set; }

    /// <exception cref="DirectoryNotFoundException">Library for Manga not loaded</exception>
    [NotMapped]
    public string? FullArchiveFilePath => GetFullFilepath();

    private static readonly Regex ChapterNumberRegex = new(@"(?:\d+\.)*\d+", RegexOptions.Compiled);
    public Chapter(Manga parentManga, string chapterNumber,
        int? volumeNumber, string? title = null)
        : base(TokenGen.CreateToken(typeof(Chapter), parentManga.Key, chapterNumber))
    {
        if(ChapterNumberRegex.Match(chapterNumber) is not { Success: true } match || !match.Value.Equals(chapterNumber))
            throw new ArgumentException($"Invalid chapter number: {chapterNumber}");
        chapterNumber = string.Join('.', chapterNumber.Split('.').Select(p => int.Parse(p).ToString()));
        this.ChapterNumber = chapterNumber;
        this.ParentManga = parentManga;
        this.MangaConnectorIds = [];
        this.VolumeNumber = volumeNumber;
        this.Title = title;
        this.Downloaded = false;
        this.MangaConnectorIds = [];
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal Chapter(string key, int? volumeNumber, string chapterNumber, string? title, string? fileName, bool downloaded)
        : base(key)
    {
        this.VolumeNumber = volumeNumber;
        this.ChapterNumber = chapterNumber;
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


    private readonly Regex _chRex = new Regex(@"C(?:(?:h?\.?)|(?:hapter\.?))?.?([0-9]+)");
    /// <summary>
    /// Checks the filesystem if an archive at the ArchiveFilePath exists
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns>True if archive exists on disk</returns>
    /// <exception cref="KeyNotFoundException">Unable to load Chapter, Parent or Library</exception>
    public async Task<bool> CheckDownloaded(MangaContext context, CancellationToken? token = null)
    {
        if(await context.Chapters
               .Include(c => c.ParentManga)
               .ThenInclude(p => p.Library)
               .FirstOrDefaultAsync(c => c.Key == this.Key, token??CancellationToken.None) is not { } chapter)
            throw new KeyNotFoundException("Unable to find chapter");

        if (chapter.ParentManga.Library is null || (chapter.FileName is null && Constants.DownloadedChaptersCheckMatchExactName))
        {
            this.Downloaded = false;
            this.FileName = null;
            return false;
        }
        
        if (File.Exists(chapter.FullArchiveFilePath))
        {
            this.Downloaded = true;
            this.FileName = new FileInfo(chapter.FullArchiveFilePath).Name;
        }else if (Constants.DownloadedChaptersCheckMatchExactName)
        {
            this.Downloaded = false;
            this.FileName = null;
        }else
        {
            string directoryPath = chapter.ParentManga.FullDirectoryPath;
            if (!Directory.Exists(directoryPath))
            {
                this.Downloaded = false;
                return false;
            }

            string? existingFile = Directory.EnumerateFiles(directoryPath).Select(path => new FileInfo(path).Name).FirstOrDefault(file =>
            {
                double similarity = NeedlemanWunschStringUtil.CalculateSimilarityPercentage(file, this.FileName ?? GetArchiveFileName());
                if (similarity > 90)
                    return true;

                Match chMatch = _chRex.Match(file);
                if (!chMatch.Groups[1].Success)
                    return false;
                return chMatch.Groups[1].Value == this.ChapterNumber;
            });
            this.Downloaded = existingFile is not null;
            this.FileName = existingFile is not null ? new FileInfo(existingFile).Name : null;
        }
        
        await context.Sync(token??CancellationToken.None, GetType(), $"CheckDownloaded {this} {this.Downloaded}");
        return this.Downloaded;
    } 
    
    /// Placeholders:
    /// %M Obj Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Obj Internal ID
    /// %Y Year (Obj)
    private static readonly Regex NullableRex = new(@"\?([a-zA-Z])\(([^\)]*)\)|(.+?)");
    private static readonly Regex ReplaceRexx = new(@"%([a-zA-Z])|(.+?)");
    /// <summary>
    /// Returns the formatted Filename of the Archive for this chapter. Formatting is done according to <see cref="TrangaSettings.ChapterNamingScheme"/>
    /// </summary>
    /// <returns>A filename</returns>
    private string GetArchiveFileName()
    {
        string archiveNamingScheme = Tranga.Settings.ChapterNamingScheme;
        StringBuilder stringBuilder = new();
        foreach (Match nullable in NullableRex.Matches(archiveNamingScheme))
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
                'V' => VolumeNumber?.ToString() ?? (Constants.ZeroVolumeInFilenameIfNull ? "0" : null),
                'C' => ChapterNumber,
                'T' => Title,
                'A' => ParentManga?.Authors?.FirstOrDefault()?.AuthorName,
                'Y' => ParentManga?.Year.ToString(),
                _ => null
            };
            stringBuilder.Append(value);
        }

        stringBuilder.Append(".cbz");

        return stringBuilder.ToString().CleanNameForWindows();
    }

    private string? GetFullFilepath()
    {
        try
        {
            return Path.Join(ParentManga.FullDirectoryPath, this.FileName is null ? GetArchiveFileName() : FileName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public class ChapterComparer : IComparer<Chapter>
    {
        public int Compare(Chapter? x, Chapter? y)
        {
            if (x is null && y is null)
                return 0;
            if(x is null)
                return -1;
            if (y is null)
                return 1;
            return CompareChapterNumbers(x.ChapterNumber, y.ChapterNumber);
        }
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
        if(ParentManga.Description != string.Empty)
            comicInfo.Add(new XElement("Summary", ParentManga.Description));
        return comicInfo.ToString();
    }

    public override string ToString() => $"{base.ToString()} Vol.{VolumeNumber} Ch.{ChapterNumber} - {Title}";
}