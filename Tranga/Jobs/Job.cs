using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public abstract class Job : GlobalBase
{
    public MangaConnector mangaConnector { get; init; }
    public ProgressToken progressToken { get; private set; }
    public bool recurring { get; init; }
    public TimeSpan? recurrenceTime { get; set; }
    public DateTime? lastExecution { get; private set; }
    public DateTime nextExecution => NextExecution();
    public string id => GetId();
    internal IEnumerable<Job>? subJobs { get; private set; }
    public string? parentJobId { get; init; }

    internal Job(GlobalBase clone, MangaConnector connector, bool recurring = false, TimeSpan? recurrenceTime = null, string? parentJobId = null) : base(clone)
    {
        this.mangaConnector = connector;
        this.progressToken = new ProgressToken(0);
        this.recurring = recurring;
        if (recurring && recurrenceTime is null)
            throw new ArgumentException("If recurrence is set to true, a recurrence time has to be provided.");
        else if(recurring && recurrenceTime is not null)
            this.lastExecution = DateTime.Now.Subtract((TimeSpan)recurrenceTime);
        this.recurrenceTime = recurrenceTime ?? TimeSpan.Zero;
        this.parentJobId = parentJobId;
    }

    internal Job(GlobalBase clone, MangaConnector connector, DateTime lastExecution, bool recurring = false,
        TimeSpan? recurrenceTime = null, string? parentJobId = null) : base(clone)
    {
        this.mangaConnector = connector;
        this.progressToken = new ProgressToken(0);
        this.recurring = recurring;
        if (recurring && recurrenceTime is null)
            throw new ArgumentException("If recurrence is set to true, a recurrence time has to be provided.");
        this.lastExecution = lastExecution;
        this.recurrenceTime = recurrenceTime ?? TimeSpan.Zero;
        this.parentJobId = parentJobId;
    }

    protected abstract string GetId();

    public void AddSubJob(Job job)
    {
        subJobs ??= new List<Job>();
        subJobs = subJobs.Append(job);
    }

    private DateTime NextExecution()
    {
        if(recurrenceTime.HasValue && lastExecution.HasValue)
            return lastExecution.Value.Add(recurrenceTime.Value);
        if(recurrenceTime.HasValue && !lastExecution.HasValue)
            return DateTime.Now;
        return DateTime.MaxValue;
    }

    public void ResetProgress()
    {
        this.progressToken.increments = this.progressToken.increments - this.progressToken.incrementsCompleted;
        this.lastExecution = DateTime.Now;
    }

    public void ExecutionEnqueue()
    {
        this.progressToken.increments = this.progressToken.increments - this.progressToken.incrementsCompleted;
        this.lastExecution = recurrenceTime is not null ? DateTime.Now.Subtract((TimeSpan)recurrenceTime) : DateTime.UnixEpoch;
        this.progressToken.Standby();
    }

    public void Cancel()
    {
        Log($"Cancelling {this}");
        this.progressToken.cancellationRequested = true;
        this.progressToken.Cancel();
        this.lastExecution = DateTime.Now;
        if(subJobs is not null)
            foreach(Job subJob in subJobs)
                subJob.Cancel();
    }

    public IEnumerable<Job> ExecuteReturnSubTasks()
    {
        progressToken.Start();
        subJobs = ExecuteReturnSubTasksInternal();
        lastExecution = DateTime.Now;
        return subJobs;
    }

    protected abstract IEnumerable<Job> ExecuteReturnSubTasksInternal();
}