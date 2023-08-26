using System.Text;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapters : Job
{
    public Publication publication { get; init; }
    
    public DownloadNewChapters(GlobalBase clone, MangaConnector connector, Publication publication, bool recurring = false, TimeSpan? recurrence = null) : base (clone, connector, recurring, recurrence)
    {
        this.publication = publication;
    }

    protected override string GetId()
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Concat(this.GetType().ToString(), publication.internalId)));
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        Chapter[] chapters = mangaConnector.GetNewChapters(publication);
        this.progressToken.increments = chapters.Length;
        List<Job> subJobs = new();
        foreach (Chapter chapter in chapters)
        {
            DownloadChapter downloadChapterJob = new(this, this.mangaConnector, chapter);
            subJobs.Add(downloadChapterJob);
        }
        progressToken.Complete();
        return subJobs;
    }
}