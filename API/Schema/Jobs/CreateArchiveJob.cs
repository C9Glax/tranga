using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Schema.Jobs;

public class CreateArchiveJob(string imagesLocation, string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(CreateArchiveJob), 64), JobType.CreateArchiveJob, TimeSpan.Zero, parentJobId, dependsOnJobIds)
{
    public string ImagesLocation { get; init; } = imagesLocation;

    [MaxLength(64)]
    [ForeignKey("Chapter")]
    public string ChapterId { get; init; } = chapterId;
    [JsonIgnore]internal Chapter Chapter { get; }
}