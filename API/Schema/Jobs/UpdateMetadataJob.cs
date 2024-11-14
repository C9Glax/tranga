using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateMetadataJob(JobType jobType, TimeSpan recurrence, string mangaId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(UpdateMetadataJob), 64), JobType.UpdateMetaDataJob, recurrence, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    [ForeignKey("Manga")]
    public string MangaId { get; init; } = mangaId;
    [JsonIgnore]internal Manga Manga { get; }
}