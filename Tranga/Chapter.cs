namespace Tranga;

public struct Chapter
{
    public Publication publication { get; }
    public string? name { get; }
    public string? volumeNumber { get; }
    public string? chapterNumber { get; }

    public Chapter(Publication publication, string? name, string? volumeNumber, string? chapterNumber)
    {
        this.publication = publication;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
    }
}