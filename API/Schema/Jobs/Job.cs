using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("JobId")]
public abstract class Job(string jobId, JobWorker.Jobs.Job.JobType jobType, TimeSpan recurrence, string? parentJobId = null)
{
    [MaxLength(64)]
    public string JobId { get; } = jobId;
    [ForeignKey("ParentJob")] public string? ParentJobId { get; init; } = parentJobId;
    [JsonIgnore] internal Job ParentJob { get; }
    public JobWorker.Jobs.Job.JobType JobType { get; init; } = jobType;
    public ulong RecurrenceMs { get; set; } = Convert.ToUInt64(recurrence.TotalMilliseconds);
    public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;
}