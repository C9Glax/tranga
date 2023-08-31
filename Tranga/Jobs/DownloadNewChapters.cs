using System.Text;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapters : Job
{
    public Manga manga { get; init; }
    
    public DownloadNewChapters(GlobalBase clone, MangaConnector connector, Manga manga, bool recurring = false, TimeSpan? recurrence = null) : base (clone, connector, recurring, recurrence)
    {
        this.manga = manga;
    }

    protected override string GetId()
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Concat(this.GetType().ToString(), manga.internalId)));
    }
    
    public override string ToString()
    {
        return $"DownloadChapter {id} {manga}";
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        Chapter[] chapters = mangaConnector.GetNewChapters(manga);
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