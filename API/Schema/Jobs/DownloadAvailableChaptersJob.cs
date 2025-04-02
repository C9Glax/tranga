using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class DownloadAvailableChaptersJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; } = mangaId;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        return context.Chapters.Where(c => c.ParentMangaId == MangaId).AsEnumerable()
            .Select(chapter => new DownloadSingleChapterJob(chapter.ChapterId, this.JobId));
    }
}