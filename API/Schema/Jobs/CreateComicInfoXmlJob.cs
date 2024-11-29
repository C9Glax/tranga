using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class CreateComicInfoXmlJob(string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob),64), JobType.CreateComicInfoXmlJob, 0, parentJobId, dependsOnJobIds)
{
    
    [MaxLength(64)]
    public string ChapterId { get; init; } = chapterId;
    public virtual Chapter Chapter { get; init; }
}