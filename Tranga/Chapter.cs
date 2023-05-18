namespace Tranga;

public struct Chapter
{
    public Publication publication { get; }
    public string? name { get; }
    public string? volumeNumber { get; }
    public string? chapterNumber { get; }
    public string url { get; }
    
    public string fileName { get; }

    public Chapter(Publication publication, string? name, string? volumeNumber, string? chapterNumber, string url, string fileName)
    {
        this.publication = publication;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.url = url;
        this.fileName = fileName;
    }
}