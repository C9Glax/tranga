namespace Tranga;

public abstract class Connector
{
    public abstract string name { get; }
    public abstract bool GetPublications(out Publication[] publications);
    public abstract bool GetChapters(Publication publication, out Chapter[] chapters);
    public abstract bool DownloadChapter(Chapter chapter); //where to?
}