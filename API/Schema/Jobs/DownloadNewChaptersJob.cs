using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadNewChaptersJob(JobType jobType, TimeSpan recurrence, string mangaId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob), 64), JobType.DownloadNewChaptersJob, recurrence, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    [ForeignKey("Manga")]
    public string MangaId { get; init; } = mangaId;
    [JsonIgnore]public Manga Manga { get; init; }
}