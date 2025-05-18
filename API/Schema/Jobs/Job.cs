using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Schema.Contexts;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("JobId")]
public abstract class Job 
{
    [StringLength(64)]
    [Required]
    public string JobId { get; init; }

    [StringLength(64)] public string? ParentJobId { get; private set; }
    [JsonIgnore] public Job? ParentJob { get; internal set; }
    private ICollection<Job> _dependsOnJobs = null!;
    [JsonIgnore] public ICollection<Job> DependsOnJobs
    {
        get => LazyLoader.Load(this, ref _dependsOnJobs);
        init => _dependsOnJobs = value;
    }

    [Required] public JobType JobType { get; init; }

    [Required] public ulong RecurrenceMs { get; set; }

    [Required] public DateTime LastExecution { get; internal set; } = DateTime.UnixEpoch;

    [NotMapped] [Required] public DateTime NextExecution => LastExecution.AddMilliseconds(RecurrenceMs);
    [Required] public JobState state { get; internal set; } = JobState.FirstExecution;
    [Required] public bool Enabled { get; internal set; } = true;

    [JsonIgnore] [NotMapped] internal bool IsCompleted => state is >= (JobState)128 and < (JobState)192;

    [NotMapped] [JsonIgnore] protected ILog Log { get; init; }
    [NotMapped] [JsonIgnore] protected ILazyLoader LazyLoader { get; init; }

    protected Job(string jobId, JobType jobType, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
    {
        this.JobId = jobId;
        this.JobType = jobType;
        this.RecurrenceMs = recurrenceMs;
        this.ParentJobId = parentJob?.JobId;
        this.ParentJob = parentJob;
        this.DependsOnJobs = dependsOnJobs ?? [];
        
        this.Log = LogManager.GetLogger(this.GetType());
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    protected internal Job(ILazyLoader lazyLoader, string jobId, JobType jobType, ulong recurrenceMs, string? parentJobId)
    {
        this.LazyLoader = lazyLoader;
        this.JobId = jobId;
        this.JobType = jobType;
        this.RecurrenceMs = recurrenceMs;
        this.ParentJobId = parentJobId;
        this.DependsOnJobs = [];
        
        this.Log = LogManager.GetLogger(this.GetType());
    }

    public IEnumerable<Job> Run(PgsqlContext context)
    {
        Log.Info($"Running job {JobId}");
        DateTime jobStart = DateTime.UtcNow;
        context.Attach(this);
        Job[]? ret = null;

        try
        {
            this.state = JobState.Running;
            context.SaveChanges();
            ret = RunInternal(context).ToArray();
            this.state = this.RecurrenceMs > 0 ? JobState.CompletedWaiting : JobState.Completed;
            this.LastExecution = DateTime.UtcNow;
            context.Jobs.AddRange(ret);
            Log.Info($"Job {JobId} completed. Generated {ret.Length} new jobs.");
            context.SaveChanges();
        }
        catch (Exception e)
        {
            if (e is not DbUpdateException)
            {
                this.state = JobState.Failed;
                this.Enabled = false;
                this.LastExecution = DateTime.UtcNow;
                Log.Error($"Failed to run job {JobId}", e);
                context.SaveChanges();
            }
            else
            {
                Log.Error($"Failed to update Database {JobId}", e);
            }
        }
        
        Log.Info($"Finished Job {JobId}! (took {DateTime.UtcNow.Subtract(jobStart).TotalMilliseconds}ms)");
        return ret ?? [];
    }
    
    protected abstract IEnumerable<Job> RunInternal(PgsqlContext context);

    public override string ToString()
    {
        return $"{JobId}";
    }
}