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
    public string? volumeNumber { get; }
    public string chapterNumber { get; }
    public string url { get; }
    // ReSharper disable once MemberCanBePrivate.Global
    public string fileName { get; }
    
    private static readonly Regex LegalCharacters = new (@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");
    private static readonly Regex IllegalStrings = new(@"Vol(ume)?.?", RegexOptions.IgnoreCase);
    public Chapter(Manga parentManga, string? name, string? volumeNumber, string chapterNumber, string url)
    {
        this.parentManga = parentManga;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;

        string chapterName = string.Concat(LegalCharacters.Matches(name ?? ""));
        string volStr = this.volumeNumber is not null ? $"Vol.{this.volumeNumber} " : "";
        string chNumberStr = $"Ch.{chapterNumber} ";
        string chNameStr = chapterName.Length > 0 ? $"- {chapterName}" : "";
        chNameStr = IllegalStrings.Replace(chNameStr, "");
        this.fileName = $"{volStr}{chNumberStr}{chNameStr}";
    }

    public override string ToString()
    {
        return $"Chapter {parentManga.sortName} {parentManga.internalId} {chapterNumber} {name}";
    }

    public int CompareTo(object? obj)
    {
        if (obj is Chapter otherChapter)
        {
            if (float.TryParse(volumeNumber, GlobalBase.numberFormatDecimalPoint, out float volumeNumberFloat) &&
                float.TryParse(chapterNumber, GlobalBase.numberFormatDecimalPoint, out float chapterNumberFloat) &&
                float.TryParse(otherChapter.volumeNumber, GlobalBase.numberFormatDecimalPoint,
                    out float otherVolumeNumberFloat) &&
                float.TryParse(otherChapter.chapterNumber, GlobalBase.numberFormatDecimalPoint,
                    out float otherChapterNumberFloat))
            {

                switch (volumeNumberFloat.CompareTo(otherVolumeNumberFloat))
                {
                    case < 0:
                        return -1;
                    case > 0:
                        return 1;
                    default:
                        return chapterNumberFloat.CompareTo(otherChapterNumberFloat);
                }
            }
            else throw new FormatException($"Value could not be parsed");
        }
        throw new ArgumentException($"{obj} can not be compared to {this}");
    }

    /// <summary>
    /// Checks if a chapter-archive is already present
    /// </summary>
    /// <returns>true if chapter is present</returns>
    internal bool CheckChapterIsDownloaded(string downloadLocation)
    {
        string newFilePath = GetArchiveFilePath(downloadLocation);
        if (!Directory.Exists(Path.Join(downloadLocation, parentManga.folderName)))
            return false;
        FileInfo[] archives = new DirectoryInfo(Path.Join(downloadLocation, parentManga.folderName)).GetFiles();
        Regex chapterInfoRex = new(@"Ch\.[0-9.]+");
        Regex chapterRex = new(@"[0-9]+(\.[0-9]+)?");
        
        if (File.Exists(newFilePath))
            return true;

        string cn = this.chapterNumber;
        if (archives.FirstOrDefault(archive => chapterRex.Match(chapterInfoRex.Match(archive.Name).Value).Value == cn) is { } path)
        {
            File.Move(path.FullName, newFilePath);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Creates full file path of chapter-archive
    /// </summary>
    /// <returns>Filepath</returns>
    internal string GetArchiveFilePath(string downloadLocation)
    {
        return Path.Join(downloadLocation, parentManga.folderName, $"{parentManga.folderName} - {this.fileName}.cbz");
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