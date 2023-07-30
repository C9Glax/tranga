using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tranga;

/// <summary>
/// Has to be Part of a publication
/// Includes the Chapter-Name, -VolumeNumber, -ChapterNumber, the location of the chapter on the internet and the saveName of the local file.
/// </summary>
public readonly struct Chapter
{
    public Publication parentPublication { get; }
    public string? name { get; }
    public string? volumeNumber { get; }
    public string chapterNumber { get; }
    public string url { get; }
    public string fileName { get; }
    
    private static readonly Regex LegalCharacters = new (@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");
    private static readonly Regex IllegalStrings = new(@"Vol(ume)?.?", RegexOptions.IgnoreCase);
    public Chapter(Publication parentPublication, string? name, string? volumeNumber, string chapterNumber, string url)
    {
        this.parentPublication = parentPublication;
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
    
    
    /// <summary>
    /// Checks if a chapter-archive is already present
    /// </summary>
    /// <returns>true if chapter is present</returns>
    internal bool CheckChapterIsDownloaded(string downloadLocation)
    {
        string newFilePath = GetArchiveFilePath(downloadLocation);
        if (!Directory.Exists(Path.Join(downloadLocation, parentPublication.folderName)))
            return false;
        FileInfo[] archives = new DirectoryInfo(Path.Join(downloadLocation, parentPublication.folderName)).GetFiles();
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
        return Path.Join(downloadLocation, parentPublication.folderName, $"{parentPublication.folderName} - {this.fileName}.cbz");
    }

    /// <summary>
    /// Creates a string containing XML of publication and chapter.
    /// See ComicInfo.xml
    /// </summary>
    /// <returns>XML-string</returns>
    internal string GetComicInfoXmlString()
    {
        XElement comicInfo = new XElement("ComicInfo",
            new XElement("Tags", string.Join(',', parentPublication.tags)),
            new XElement("LanguageISO", parentPublication.originalLanguage),
            new XElement("Title", this.name),
            new XElement("Writer", string.Join(',', parentPublication.authors)),
            new XElement("Volume", this.volumeNumber),
            new XElement("Number", this.chapterNumber)
        );
        return comicInfo.ToString();
    }
}