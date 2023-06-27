using System.Globalization;
using System.Text.RegularExpressions;

namespace Tranga;

/// <summary>
/// Has to be Part of a publication
/// Includes the Chapter-Name, -VolumeNumber, -ChapterNumber, the location of the chapter on the internet and the saveName of the local file.
/// </summary>
public struct Chapter
{
    public string? name { get; }
    public string? volumeNumber { get; }
    public string? chapterNumber { get; }
    public string url { get; }
    public string fileName { get; }
    
    private static readonly Regex LegalCharacters = new Regex(@"([A-z]*[0-9]* *\.*-*,*\]*\[*'*\'*\)*\(*~*!*)*");
    public Chapter(string? name, string? volumeNumber, string? chapterNumber, string url)
    {
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;

        string chapterName = string.Concat(LegalCharacters.Matches(name ?? ""));
        string volStr = this.volumeNumber is not null ? $"Vol.{this.volumeNumber} " : "";
        string chNumberStr = this.chapterNumber is not null ? $"Ch.{chapterNumber} " : "";
        string chNameStr = chapterName.Length > 0 ? $"- {chapterName}" : "";
        chNameStr = chNameStr.Replace("Volume", "").Replace("volume", "");
        this.fileName = $"{volStr}{chNumberStr}{chNameStr}";
    }
}