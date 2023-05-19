using System.Globalization;

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

    public Chapter(string? name, string? volumeNumber, string? chapterNumber, string url)
    {
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;
        string chapterName = string.Concat((name ?? "").Split(Path.GetInvalidFileNameChars()));
        this.fileName = $"{chapterName} - V{volumeNumber}C{chapterNumber} - {volumeNumber}{chapterNumber}";
    }
}