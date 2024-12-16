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
    public string? ParentJobId { get; internal set; }
    internal virtual Job ParentJob { get; }
    
    [MaxLength(64)]
    public string[]? DependsOnJobIds { get; init; }
    public virtual Job[] DependsOnJobs { get; init; }
    
    public JobType JobType { get; init; }
    public ulong RecurrenceMs { get; set; }
    public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;
    public DateTime NextExecution { get; internal set; } 
    public JobState state { get; internal set; } = JobState.Waiting;

    public Job(string jobId, JobType jobType, ulong recurrenceMs, string? parentJobId = null,
        string[]? dependsOnJobIds = null)
    {
        JobId = jobId;
        ParentJobId = parentJobId;
        DependsOnJobIds = dependsOnJobIds;
        JobType = jobType;
        RecurrenceMs = recurrenceMs;
        NextExecution = LastExecution.AddMilliseconds(RecurrenceMs);
    }

    public IEnumerable<Job> Run(PgsqlContext context)
    {
        this.state = JobState.Running;
        IEnumerable<Job> newJobs = RunInternal(context);
        this.state = JobState.Completed;
        return newJobs;
    }
    
    protected abstract IEnumerable<Job> RunInternal(PgsqlContext context);
}