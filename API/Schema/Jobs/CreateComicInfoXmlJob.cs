using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Schema.Jobs;

public class CreateComicInfoXmlJob(string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob),64), JobType.CreateComicInfoXmlJob, TimeSpan.Zero, parentJobId, dependsOnJobIds)
{
    
    [MaxLength(64)]
    [ForeignKey("Chapter")]
    public string ChapterId { get; init; } = chapterId;
    [JsonIgnore]internal Chapter Chapter { get; }
}