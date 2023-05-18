namespace Tranga;

public struct Chapter
{
    public Publication publication { get; }
    public string name { get; }
    public uint volumeNumber { get; }
    public uint chapterNumber { get; }
    public string summary { get; }
    public string posterUrl { get; }//Better way?

    public Chapter(Publication publication, string name, uint volumeNumber, uint chapterNumber, string summary,
        string posterUrl)
    {
        this.publication = publication;
        this.name = name;
        this.volumeNumber = volumeNumber;
        this.chapterNumber = chapterNumber;
        this.summary = summary;
        this.posterUrl = posterUrl;
    }
}