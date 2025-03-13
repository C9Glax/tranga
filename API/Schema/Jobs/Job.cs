using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("JobId")]
public abstract class Job 
{
    [StringLength(64)]
    [Required]
    public string JobId { get; init; }
    [StringLength(64)]
    public string? ParentJobId { get; init; }
    [JsonIgnore]
    public Job? ParentJob { get; init; }
    [StringLength(64)]
    public ICollection<string>? DependsOnJobsIds { get; init; }
    [JsonIgnore]
    public ICollection<Job>? DependsOnJobs { get; init; }
    
    [Required]
    public JobType JobType { get; init; }
    [Required]
    public ulong RecurrenceMs { get; set; }
    [Required]
    public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;
    
    [NotMapped]
    [Required]
    public DateTime NextExecution => LastExecution.AddMilliseconds(RecurrenceMs);
    [Required]
    public JobState state { get; internal set; } = JobState.Waiting;
    [Required]
    public bool Enabled { get; internal set; } = true;

    public Job(string jobId, JobType jobType, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : this(jobId, jobType, recurrenceMs, parentJob?.JobId, dependsOnJobs?.Select(j => j.JobId).ToList())
    {
        this.ParentJob = parentJob;
        this.DependsOnJobs = dependsOnJobs;
    }

    public Job(string jobId, JobType jobType, ulong recurrenceMs, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    {
        JobId = jobId;
        ParentJobId = parentJobId;
        DependsOnJobsIds = dependsOnJobsIds;
        JobType = jobType;
        RecurrenceMs = recurrenceMs;
    }

    public IEnumerable<Job> Run(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext context = scope.ServiceProvider.GetRequiredService<PgsqlContext>();
        
        this.state = JobState.Running;
        context.SaveChanges();
        IEnumerable<Job> newJobs = RunInternal(context);
        this.state = JobState.Completed;
        context.SaveChanges();
        return newJobs;
    }
    
    protected abstract IEnumerable<Job> RunInternal(PgsqlContext context);
}