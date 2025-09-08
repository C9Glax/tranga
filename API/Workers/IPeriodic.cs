namespace API.Workers;

public interface IPeriodic
{
    internal DateTime LastExecution { get; set; }
    public TimeSpan Interval { get; set; }
    public DateTime NextExecution => LastExecution.Add(Interval);
    public bool IsDue =>  NextExecution <= DateTime.UtcNow;
}