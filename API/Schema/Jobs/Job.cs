using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using log4net;
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
    
    [NotMapped]
    [JsonIgnore]
    protected ILog Log { get; init; }

    public Job(string jobId, JobType jobType, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : this(jobId, jobType, recurrenceMs, parentJob?.JobId, dependsOnJobs?.Select(j => j.JobId).ToList())
    {
        this.ParentJob = parentJob;
        this.DependsOnJobs = dependsOnJobs;
    }

    public Job(string jobId, JobType jobType, ulong recurrenceMs, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    {
        Log = LogManager.GetLogger(GetType());
        JobId = jobId;
        ParentJobId = parentJobId;
        DependsOnJobsIds = dependsOnJobsIds;
        JobType = jobType;
        RecurrenceMs = recurrenceMs;
    }

    public IEnumerable<Job> Run(IServiceProvider serviceProvider)
    {
        Log.Debug($"Running job {JobId}");
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext context = scope.ServiceProvider.GetRequiredService<PgsqlContext>();

        try
        {
            this.state = JobState.Running;
            context.SaveChanges();
            Job[] newJobs = RunInternal(context).ToArray();
            this.state = JobState.Completed;
            context.Jobs.AddRange(newJobs);
            context.SaveChanges();
            Log.Info($"Job {JobId} completed. Generated {newJobs.Length} new jobs.");
            return newJobs;
        }
        catch (DbUpdateException e)
        {
            this.state = JobState.Failed;
            Log.Error($"Failed to run job {JobId}", e);
            return [];
        }
    }
    
    protected abstract IEnumerable<Job> RunInternal(PgsqlContext context);
}