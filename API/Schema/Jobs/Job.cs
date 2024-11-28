using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("JobId")]
public abstract class Job
{
    [MaxLength(64)]
    public string JobId { get; init; }
    
    [MaxLength(64)]
    [ForeignKey("ParentJob")] public string? ParentJobId { get; internal set; }
    [JsonIgnore] internal Job ParentJob { get; }
    
    [MaxLength(64)]
    [ForeignKey("DependsOnJob")] public string[]? DependsOnJobIds { get; init; }
    [JsonIgnore] public Job[] DependsOnJobs { get; init; }
    
    public JobType JobType { get; init; }
    public ulong RecurrenceMs { get; set; }
    public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;
    public DateTime NextExecution { get; internal set; } 
    public JobState state { get; internal set; } = JobState.Waiting;

    public object? returnValue { get; set; } = null;

    public Job(string jobId, JobType jobType, TimeSpan recurrence, string? parentJobId = null,
        string[]? dependsOnJobIds = null)
    {
        JobId = jobId;
        ParentJobId = parentJobId;
        DependsOnJobIds = dependsOnJobIds;
        JobType = jobType;
        RecurrenceMs = Convert.ToUInt64(recurrence.TotalMilliseconds);
        NextExecution = LastExecution.AddMilliseconds(RecurrenceMs);

        foreach (Job dependsOnJob in DependsOnJobs)
            dependsOnJob.ParentJobId = this.JobId;
    }

    public void MarkCompleted()
    {
        
    }
}