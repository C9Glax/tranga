using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("JobId")]
public abstract class Job(string jobId, JobType jobType, TimeSpan recurrence, string? parentJobId = null, string[]? dependsOnJobIds = null)
{
    [MaxLength(64)]
    public string JobId { get; } = jobId;
    [MaxLength(64)]
    [ForeignKey("ParentJob")] public string? ParentJobId { get; init; } = parentJobId;
    [JsonIgnore] internal Job ParentJob { get; }
    [MaxLength(64)]
    [ForeignKey("DependsOnJob")] public string[]? DependsOnJobIds { get; init; } = dependsOnJobIds;
    [JsonIgnore] internal Job[] DependsOnJobs { get; }
    public JobType JobType { get; init; } = jobType;
    public ulong RecurrenceMs { get; set; } = Convert.ToUInt64(recurrence.TotalMilliseconds);
    public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;
    public bool Completed { get; set; } = false;
}