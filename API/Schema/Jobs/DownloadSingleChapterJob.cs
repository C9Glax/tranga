using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadSingleChapterJob(JobWorker.Jobs.Job.JobType jobType, TimeSpan recurrence, string chapterId, string? parentJobId = null)
    : Job(TokenGen.CreateToken(typeof(DownloadSingleChapterJob), 64), JobWorker.Jobs.Job.JobType.DownloadSingleChapter, recurrence, parentJobId)
{
    [MaxLength(64)]
    [ForeignKey("Chapter")]
    public string ChapterId { get; init; } = chapterId;
    [JsonIgnore]internal Chapter Chapter { get; }
}