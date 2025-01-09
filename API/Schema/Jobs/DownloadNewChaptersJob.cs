using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;

namespace API.Schema.Jobs;

public class DownloadNewChaptersJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob), 64), JobType.DownloadNewChaptersJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    public Manga? Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Manga m = Manga ?? context.Manga.Find(MangaId)!;
        MangaConnector connector = m.MangaConnector ?? context.MangaConnectors.Find(m.MangaConnectorId)!;
        Chapter[] newChapters = connector.GetNewChapters(m);
        context.Chapters.AddRangeAsync(newChapters).Wait();
        context.SaveChangesAsync().Wait();
        return newChapters.Select(chapter => new DownloadSingleChapterJob(chapter.ChapterId, this.JobId));
    }
}