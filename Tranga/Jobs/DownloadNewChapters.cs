using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapters : Job
{
    public Publication publication { get; init; }
    
    public DownloadNewChapters(MangaConnector connector, Publication publication, bool recurring = false) : base (connector, recurring)
    {
        this.publication = publication;
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        Chapter[] chapters = mangaConnector.GetNewChapters(publication);
        this.progressToken.increments = chapters.Length;
        List<Job> subJobs = new();
        foreach (Chapter chapter in chapters)
        {
            DownloadChapter downloadChapterJob = new(this.mangaConnector, chapter);
            subJobs.Add(downloadChapterJob);
        }
        progressToken.Complete();
        return subJobs;
    }
}