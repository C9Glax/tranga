using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadSingleChapterJob(string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadSingleChapterJob), 64), JobType.DownloadSingleChapterJob, TimeSpan.Zero, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    [ForeignKey("Chapter")]
    public string ChapterId { get; init; } = chapterId;
    [JsonIgnore]public Chapter Chapter { get; init; }
}