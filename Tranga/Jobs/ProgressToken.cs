namespace Tranga.Jobs;

public class ProgressToken
{
    public bool cancellationRequested { get; set; }
    public int increments { get; set; }
    public int incrementsCompleted { get; set; }
    public float progress => GetProgress();
    public DateTime lastUpdate { get; private set; }
    public DateTime executionStarted { get; private set; }
    public TimeSpan timeRemaining => GetTimeRemaining();
    
    public enum State { Running, Complete, Standby, Cancelled, Waiting }
    public State state { get; private set; }

    public ProgressToken(int increments)
    {
        this.cancellationRequested = false;
        this.increments = increments;
        this.incrementsCompleted = 0;
        this.state = State.Waiting;
        this.executionStarted = DateTime.UnixEpoch;
        this.lastUpdate = DateTime.UnixEpoch;
    }

    private float GetProgress()
    {
        if(increments > 0 && incrementsCompleted > 0)
            return (float)incrementsCompleted / (float)increments;
        return 0;
    }

    private TimeSpan GetTimeRemaining()
    {
        if (increments > 0 && incrementsCompleted > 0)
            return DateTime.Now.Subtract(this.executionStarted).Divide(incrementsCompleted).Multiply(increments - incrementsCompleted);
        return TimeSpan.MaxValue;
    }

    public void Increment()
    {
        this.lastUpdate = DateTime.Now;
        this.incrementsCompleted++;
        if (incrementsCompleted > increments)
            state = State.Complete;
    }

    public void Standby()
    {
        this.lastUpdate = DateTime.Now;
        state = State.Standby;
    }

    public void Start()
    {
        this.lastUpdate = DateTime.Now;
        state = State.Running;
        this.executionStarted = DateTime.Now;
    }

    public void Complete()
    {
        this.lastUpdate = DateTime.Now;
        state = State.Complete;
    }

    public void Cancel()
    {
        this.lastUpdate = DateTime.Now;
        state = State.Cancelled;
    }

    public void Waiting()
    {
        this.lastUpdate = DateTime.Now;
        state = State.Waiting;
    }
}