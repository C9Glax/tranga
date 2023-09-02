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

    public Job(GlobalBase clone, MangaConnector connector, bool recurring = false, TimeSpan? recurrenceTime = null) : base(clone)
    {
        this.mangaConnector = connector;
        this.progressToken = new ProgressToken(0);
        this.recurring = recurring;
        if (recurring && recurrenceTime is null)
            throw new ArgumentException("If recurrence is set to true, a recurrence time has to be provided.");
        else if(recurring && recurrenceTime is not null)
            this.lastExecution = DateTime.Now.Subtract((TimeSpan)recurrenceTime);
        this.recurrenceTime = recurrenceTime;
    }

    protected abstract string GetId();

    public Job(GlobalBase clone, MangaConnector connector, ProgressToken progressToken, bool recurring = false, TimeSpan? recurrenceTime = null) : base(clone)
    {
        this.mangaConnector = connector;
        this.progressToken = progressToken;
        this.recurring = recurring;
        if (recurring && recurrenceTime is null)
            throw new ArgumentException("If recurrence is set to true, a recurrence time has to be provided.");
        this.recurrenceTime = recurrenceTime;
    }

    public Job(GlobalBase clone, MangaConnector connector, int taskIncrements, bool recurring = false, TimeSpan? recurrenceTime = null) : base(clone)
    {
        this.mangaConnector = connector;
        this.progressToken = new ProgressToken(taskIncrements);
        this.recurring = recurring;
        if (recurring && recurrenceTime is null)
            throw new ArgumentException("If recurrence is set to true, a recurrence time has to be provided.");
        this.recurrenceTime = recurrenceTime;
    }

    private DateTime NextExecution()
    {
        if(recurring && recurrenceTime.HasValue && lastExecution.HasValue)
            return lastExecution.Value.Add(recurrenceTime.Value);
        if(recurring && recurrenceTime.HasValue && !lastExecution.HasValue)
            return DateTime.Now;
        return DateTime.MaxValue;
    }

    public void Reset()
    {
        this.progressToken = new ProgressToken(this.progressToken.increments);
    }

    public void Cancel()
    {
        Log($"Cancelling {this}");
        this.progressToken.cancellationRequested = true;
        this.progressToken.Complete();
    }

    public IEnumerable<Job> ExecuteReturnSubTasks()
    {
        progressToken.Start();
        IEnumerable<Job> ret = ExecuteReturnSubTasksInternal();
        lastExecution = DateTime.Now;
        return ret;
    }

    protected abstract IEnumerable<Job> ExecuteReturnSubTasksInternal();
}