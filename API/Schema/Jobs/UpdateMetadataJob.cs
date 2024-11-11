using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateMetadataJob(JobWorker.Jobs.Job.JobType jobType, TimeSpan recurrence, string mangaId, string? parentJobId = null)
    : Job(TokenGen.CreateToken(typeof(UpdateMetadataJob), 64), JobWorker.Jobs.Job.JobType.UpdateMetaData, recurrence, parentJobId)
{
    [MaxLength(64)]
    [ForeignKey("Manga")]
    public string MangaId { get; init; } = mangaId;
    [JsonIgnore]internal Manga Manga { get; }
}