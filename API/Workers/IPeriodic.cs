namespace API.Workers;

public interface IPeriodic<T> where T : BaseWorker
{
    protected DateTime LastExecution { get; set; }
    protected TimeSpan Interval { get; set; }
    
    public DateTime NextExecution => LastExecution.Add(Interval);
}