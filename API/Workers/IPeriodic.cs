namespace API.Workers;

public interface IPeriodic
{
    protected DateTime LastExecution { get; set; }
    public TimeSpan Interval { get; set; }
    public DateTime NextExecution => LastExecution.Add(Interval);
    public bool IsDue =>  NextExecution <= DateTime.UtcNow;
}