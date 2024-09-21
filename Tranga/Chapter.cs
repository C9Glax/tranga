using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tranga;

/// <summary>
/// Has to be Part of a publication
/// Includes the Chapter-Name, -VolumeNumber, -ChapterNumber, the location of the chapter on the internet and the saveName of the local file.
/// </summary>
public readonly struct Chapter : IComparable
{
    // ReSharper disable once MemberCanBePrivate.Global
    public Manga parentManga { get; }
    public string? name { get; }
    public string volumeNumber { get; }
    public string chapterNumber { get; }
    public string url { get; }
    // ReSharper disable once MemberCanBePrivate.Global
    public string fileName { get; }
    
    private static readonly Regex LegalCharacters = new (@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");
    private static readonly Regex IllegalStrings = new(@"(Vol(ume)?|Ch(apter)?)\.?", RegexOptions.IgnoreCase);
    private static readonly Regex Digits = new(@"[0-9\.]*");
    public Chapter(Manga parentManga, string? name, string? volumeNumber, string chapterNumber, string url)
    {
        this.parentManga = parentManga;
        this.name = name;
        this.volumeNumber = volumeNumber is not null ? string.Concat(Digits.Matches(volumeNumber).Select(x => x.Value)) : "0";
        this.chapterNumber = string.Concat(Digits.Matches(chapterNumber).Select(x => x.Value));
        this.url = url;
        
        string chapterVolNumStr;
        if (volumeNumber is not null && volumeNumber.Length > 0)
            chapterVolNumStr = $"Vol.{volumeNumber} Ch.{chapterNumber}";
        else
            chapterVolNumStr = $"Ch.{chapterNumber}";

        if (name is not null && name.Length > 0)
        {
            string chapterName = IllegalStrings.Replace(string.Concat(LegalCharacters.Matches(name)), "");
            this.fileName = $"{chapterVolNumStr} - {chapterName}";
        }
        else
            this.fileName = chapterVolNumStr;
    }

    public override string ToString()
    {
        return $"Chapter {parentManga.sortName} {parentManga.internalId} {chapterNumber} {name}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Chapter)
            return false;
        return CompareTo(obj) == 0;
    }

    public int CompareTo(object? obj)
    {
        if(obj is not Chapter otherChapter)
            throw new ArgumentException($"{obj} can not be compared to {this}");
        
        if (float.TryParse(volumeNumber, GlobalBase.numberFormatDecimalPoint, out float volumeNumberFloat) &&
            float.TryParse(chapterNumber, GlobalBase.numberFormatDecimalPoint, out float chapterNumberFloat) &&
            float.TryParse(otherChapter.volumeNumber, GlobalBase.numberFormatDecimalPoint,
                out float otherVolumeNumberFloat) &&
            float.TryParse(otherChapter.chapterNumber, GlobalBase.numberFormatDecimalPoint,
                out float otherChapterNumberFloat))
        {
            return volumeNumberFloat.CompareTo(otherVolumeNumberFloat) switch
            {
                <0 => -1,
                >0 => 1,
                _ => chapterNumberFloat.CompareTo(otherChapterNumberFloat)
            };
        }
        else throw new FormatException($"Value could not be parsed");
    }

    /// <summary>
    /// Checks if a chapter-archive is already present
    /// </summary>
    /// <returns>true if chapter is present</returns>
    internal bool CheckChapterIsDownloaded()
    {
        string mangaDirectory = Path.Join(TrangaSettings.downloadLocation, parentManga.folderName);
        if (!Directory.Exists(mangaDirectory))
            return false;
        FileInfo[] archives = new DirectoryInfo(mangaDirectory).GetFiles("*.cbz");
        Regex volChRex = new(@"(?:Vol(?:ume)?\.([0-9]+)\D*)?Ch(?:apter)?\.([0-9]+(?:\.[0-9]+)*)");

        Chapter t = this;
        string correctPath = GetArchiveFilePath();
        FileInfo? archive = archives.FirstOrDefault(archive =>
        {
            Match m = volChRex.Match(archive.Name);
            /*Uncommenting this section will only allow *Version without Volume number* -> *Version with Volume number* but not the other way
            if (m.Groups[1].Success)
                return m.Groups[1].Value == t.volumeNumber && m.Groups[2].Value == t.chapterNumber;
            else*/
                return m.Groups[2].Value == t.chapterNumber;
        });
        if(archive is not null && archive.FullName != correctPath)
            archive.MoveTo(correctPath, true);
        return (archive is not null);
    }
    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    internal string GetArchiveFilePath()
    {
        return Path.Join(TrangaSettings.downloadLocation, parentManga.folderName, $"{parentManga.folderName} - {this.fileName}.cbz");
    }

    /// <summary>
    /// Creates a string containing XML of publication and chapter.
    /// See ComicInfo.xml
    /// </summary>
    /// <returns>XML-string</returns>
    internal string GetComicInfoXmlString()
    {
        XElement comicInfo = new XElement("ComicInfo",
            new XElement("Tags", string.Join(',', parentManga.tags)),
            new XElement("LanguageISO", parentManga.originalLanguage),
            new XElement("Title", this.name),
            new XElement("Writer", string.Join(',', parentManga.authors)),
            new XElement("Volume", this.volumeNumber),
            new XElement("Number", this.chapterNumber)
        );
        return comicInfo.ToString();
    }
}