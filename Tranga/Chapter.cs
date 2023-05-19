using System.Globalization;

namespace Tranga;

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
        double multiplied = Convert.ToDouble(chapterNumber, new NumberFormatInfo() { NumberDecimalSeparator = "." }) *
                            Convert.ToInt32(volumeNumber);
        this.fileName = $"{chapterName} - V{volumeNumber}C{chapterNumber} - {multiplied}";
    }
}