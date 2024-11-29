using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class CreateArchiveJob(string imagesLocation, string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(CreateArchiveJob), 64), JobType.CreateArchiveJob, 0, parentJobId, dependsOnJobIds)
{
    public string ImagesLocation { get; init; } = imagesLocation;

    [MaxLength(64)]
    public string ChapterId { get; init; } = chapterId;
    public virtual Chapter Chapter { get; init; }
}