namespace Tranga;

public struct Chapter
{
    public Publication publication { get; }
    public string? name { get; }
    public string? volumeNumber { get; }
    public string? chapterNumber { get; }
    public string url { get; }
    
    public string relativeFilePath { get; }

    public Chapter(Publication publication, string? name, string? volumeNumber, string? chapterNumber, string url, string relativeFilePath)
    {
        this.publication = publication;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;
        this.relativeFilePath = relativeFilePath;
    }
}