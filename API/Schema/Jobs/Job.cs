using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.Schema.Contexts;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

[PrimaryKey("Key")]
public abstract class Job : Identifiable, IComparable<Job>
{
    [StringLength(64)] public string? ParentJobId { get; private set; }
    [JsonIgnore] public Job? ParentJob { get; internal set; }
    private ICollection<Job>? _dependsOnJobs;
    [JsonIgnore] public ICollection<Job> DependsOnJobs
    {
        get => LazyLoader.Load(this, ref _dependsOnJobs) ?? throw new InvalidOperationException();
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
    [NotMapped] [JsonIgnore] protected ILazyLoader LazyLoader { get; init; } = null!;

    protected Job(string key, JobType jobType, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(key)
    {
        this.JobType = jobType;
        this.RecurrenceMs = recurrenceMs;
        this.ParentJobId = parentJob?.Key;
        this.ParentJob = parentJob;
        this.DependsOnJobs = dependsOnJobs ?? [];
        
        this.Log = LogManager.GetLogger(this.GetType());
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    protected internal Job(ILazyLoader lazyLoader, string key, JobType jobType, ulong recurrenceMs, string? parentJobId)
        : base(key)
    {
        this.LazyLoader = lazyLoader;
        this.JobType = jobType;
        this.RecurrenceMs = recurrenceMs;
        this.ParentJobId = parentJobId;
        this.DependsOnJobs = [];
        
        this.Log = LogManager.GetLogger(this.GetType());
    }

    public IEnumerable<Job> Run(PgsqlContext context, ref bool running)
    {
        Log.Info($"Running job {this}");
        DateTime jobStart = DateTime.UtcNow;
        Job[]? ret = null;

        try
        {
            this.state = JobState.Running;
            context.SaveChanges();
            running = true;
            ret = RunInternal(context).ToArray();
            Log.Info($"Job {this} completed. Generated {ret.Length} new jobs.");
            this.state = this.RecurrenceMs > 0 ? JobState.CompletedWaiting : JobState.Completed;
            this.LastExecution = DateTime.UtcNow;
            context.SaveChanges();
        }
        catch (Exception e)
        {
            if (e is not DbUpdateException)
            {
                Log.Error($"Failed to run job {this}", e);
                this.state = JobState.Failed;
                this.Enabled = false;
                this.LastExecution = DateTime.UtcNow;
                context.SaveChanges();
            }
            else
            {
                Log.Error($"Failed to update Database {this}", e);
            }
        }

        try
        {
            if (ret != null)
            {
                context.Jobs.AddRange(ret);
                context.SaveChanges();
            }
        }
        catch (DbUpdateException e)
        {
            Log.Error($"Failed to update Database {this}", e);
        }
        
        Log.Info($"Finished Job {this}! (took {DateTime.UtcNow.Subtract(jobStart).TotalMilliseconds}ms)");
        return ret ?? [];
    }
    
    protected abstract IEnumerable<Job> RunInternal(PgsqlContext context);

    public List<Job> GetDependenciesAndSelf()
    {
        List<Job> ret = GetDependencies();
        ret.Add(this);
        return ret;
    }

    public List<Job> GetDependencies()
    {
        List<Job> ret = new ();
        foreach (Job job in DependsOnJobs)
        {
            ret.AddRange(job.GetDependenciesAndSelf());
        }
        return ret;
    }

    public int CompareTo(Job? other)
    {
        if (other is null)
            return -1;
        // Sort by missing dependencies
        if (this.GetDependencies().Count(job => !job.IsCompleted) <
            other.GetDependencies().Count(job => !job.IsCompleted))
            return -1;
        // Sort by NextExecution-time
        if (this.NextExecution < other.NextExecution)
            return -1;
        return 1;
    }

    public override string ToString() => base.ToString();
}