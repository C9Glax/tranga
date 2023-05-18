using System.Globalization;

namespace Tranga;

public struct Chapter
{
    public Publication publication { get; }
    public string? name { get; }
    public string? volumeNumber { get; }
    public string? chapterNumber { get; }
    public string url { get; }
    
    public string fileName { get; }

    public Chapter(Publication publication, string? name, string? volumeNumber, string? chapterNumber, string url)
    {
        this.publication = publication;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;
        string chapterName = string.Concat((name ?? "").Split(Path.GetInvalidFileNameChars()));
        double multiplied = Convert.ToDouble(chapterName, new NumberFormatInfo() { NumberDecimalSeparator = "." }) *
                            Convert.ToInt32(volumeNumber);
        this.fileName = $"{chapterName} - V{volumeNumber}C{chapterNumber} - {multiplied}";
    }
}