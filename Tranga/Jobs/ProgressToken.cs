namespace Tranga.Jobs;

public class ProgressToken
{
    public bool cancellationRequested { get; set; }
    public int increments { get; set; }
    public int incrementsCompleted { get; set; }
    public float progress => GetProgress();
    
    public enum State { Running, Complete, Standby, Cancelled }
    public State state { get; private set; }

    public ProgressToken(int increments)
    {
        this.cancellationRequested = false;
        this.increments = increments;
        this.incrementsCompleted = 0;
        this.state = State.Complete;
    }

    private float GetProgress()
    {
        if(increments > 0 && incrementsCompleted > 0)
            return (float)incrementsCompleted / (float)increments;
        return 0;
    }

    public void Increment()
    {
        this.incrementsCompleted++;
        if (incrementsCompleted > increments)
            state = State.Complete;
    }

    public void Standby()
    {
        state = State.Standby;
    }

    public void Start()
    {
        state = State.Running;
    }

    public void Complete()
    {
        state = State.Complete;
    }

    public void Cancel()
    {
        state = State.Cancelled;
    }
}