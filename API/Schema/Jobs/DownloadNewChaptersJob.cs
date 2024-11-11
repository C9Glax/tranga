using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadNewChaptersJob(JobWorker.Jobs.Job.JobType jobType, TimeSpan recurrence, string mangaId, string? parentJobId = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob), 64), JobWorker.Jobs.Job.JobType.DownloadNewChapters, recurrence, parentJobId)
{
    [MaxLength(64)]
    [ForeignKey("Manga")]
    public string MangaId { get; init; } = mangaId;
    [JsonIgnore]internal Manga Manga { get; }
}