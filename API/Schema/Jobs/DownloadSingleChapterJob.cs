using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadSingleChapterJob(string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadSingleChapterJob), 64), JobType.DownloadSingleChapterJob, 0, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    public string ChapterId { get; init; } = chapterId;
    public virtual Chapter Chapter { get; init; }
}