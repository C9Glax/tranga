using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateFilesDownloadedJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(UpdateFilesDownloadedJob)), JobType.UpdateFilesDownloadedJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; } = mangaId;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        IQueryable<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == MangaId);
        foreach (Chapter chapter in chapters)
            chapter.Downloaded = chapter.IsDownloaded();

        context.SaveChanges();
        return [];
    }
}