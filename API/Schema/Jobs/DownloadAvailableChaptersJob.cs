using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadAvailableChaptersJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    
    [JsonIgnore]
    public Manga? Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        return context.Chapters.Where(c => c.ParentMangaId == MangaId).AsEnumerable()
            .Select(chapter => new DownloadSingleChapterJob(chapter.ChapterId, this.JobId));
    }
}