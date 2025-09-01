using API.Schema;
using log4net;
using Newtonsoft.Json;

namespace API.Workers;

public abstract class BaseWorker : Identifiable
{
    /// <summary>
    /// Workers this Worker depends on being completed before running.
    /// </summary>
    public BaseWorker[] DependsOn { get; init; }
    /// <summary>
    /// Dependencies and dependencies of dependencies. See also <see cref="DependsOn"/>.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<BaseWorker> AllDependencies => DependsOn.Select(d => d.AllDependencies).SelectMany(x => x);
    /// <summary>
    /// <see cref="AllDependencies"/> and Self.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<BaseWorker> DependenciesAndSelf => AllDependencies.Append(this);
    /// <summary>
    /// <see cref="DependsOn"/> where <see cref="WorkerExecutionState"/> is less than Completed.
    /// </summary>
    public IEnumerable<BaseWorker> MissingDependencies => DependsOn.Where(d => d.State < WorkerExecutionState.Completed);
    public bool AllDependenciesFulfilled => DependsOn.All(d => d.State >= WorkerExecutionState.Completed);
    internal WorkerExecutionState State { get; private set; }
    protected CancellationTokenSource CancellationTokenSource = new ();
    protected ILog Log { get; init; }

    /// <summary>
    /// Stops worker, and marks as <see cref="WorkerExecutionState"/>.Cancelled
    /// </summary>
    public void Cancel()
    {
        Log.Debug($"Cancelled {this}");
        this.State = WorkerExecutionState.Cancelled;
        CancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Stops worker, and marks as <see cref="WorkerExecutionState"/>.Failed
    /// </summary>
    protected void Fail()
    {
        Log.Debug($"Failed {this}");
        this.State = WorkerExecutionState.Failed;
        CancellationTokenSource.Cancel();
    }

    public BaseWorker(IEnumerable<BaseWorker>? dependsOn = null)
    {
        this.DependsOn = dependsOn?.ToArray() ?? [];
        this.Log = LogManager.GetLogger(GetType());
    }

    /// <summary>
    /// Sets States during worker-run.
    /// States:
    /// <list type="bullet">
    /// <item><see cref="WorkerExecutionState"/>.Waiting when waiting for <see cref="MissingDependencies"/></item>
    /// <item><see cref="WorkerExecutionState"/>.Running when running</item>
    /// <item><see cref="WorkerExecutionState"/>.Completed after finished</item>
    /// </list>
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item>If <see cref="BaseWorker"/> has <see cref="MissingDependencies"/>, missing dependencies.</item>
    /// <item>If <see cref="MissingDependencies"/> are <see cref="WorkerExecutionState"/>.Running, itself after waiting for dependencies.</item>
    /// <item>If <see cref="BaseWorker"/> has run, additional <see cref="BaseWorker"/>.</item>
    /// </list>
    /// </returns>
    public Task<BaseWorker[]> DoWork(Action? callback = null)
    {
        // Start the worker
        Log.Debug($"Checking {this}");
        this.CancellationTokenSource = new(TimeSpan.FromMinutes(10));
        this.State = WorkerExecutionState.Waiting;
        
        // Wait for dependencies, start them if necessary
        BaseWorker[] missingDependenciesThatNeedStarting = MissingDependencies.Where(d => d.State < WorkerExecutionState.Waiting).ToArray();
        if(missingDependenciesThatNeedStarting.Any())
            return new Task<BaseWorker[]>(() => missingDependenciesThatNeedStarting);

        if (MissingDependencies.Any())
            return new Task<BaseWorker[]>(WaitForDependencies);
        
        // Run the actual work
        Log.Info($"Running {this}");
        DateTime startTime = DateTime.UtcNow;
        Task<BaseWorker[]> task = DoWorkInternal();
        task.GetAwaiter().OnCompleted(Finish(startTime, callback));
        this.State = WorkerExecutionState.Running;
        return task;
    }

    private Action Finish(DateTime startTime, Action? callback = null) => () =>
    {
        DateTime endTime = DateTime.UtcNow;
        Log.Info($"Completed {this}\n\t{endTime.Subtract(startTime).TotalMilliseconds} ms");
        this.State = WorkerExecutionState.Completed;
        if(this is IPeriodic periodic)
            periodic.LastExecution = DateTime.UtcNow;
        callback?.Invoke();
    };
    
    protected abstract Task<BaseWorker[]> DoWorkInternal();

    private BaseWorker[] WaitForDependencies()
    {
        Log.Info($"Waiting for {MissingDependencies.Count()} Dependencies {this}:\n\t{string.Join("\n\t", MissingDependencies.Select(d => d.ToString()))}");
        while (CancellationTokenSource.IsCancellationRequested == false && MissingDependencies.Any())
        {
            Thread.Sleep(Tranga.Settings.WorkCycleTimeoutMs);  
        }
        return [this];
    }
}

public enum WorkerExecutionState
{
    Failed = 0,
    Cancelled = 32,
    Created = 64,
    Waiting = 96,
    Running = 128,
    Completed = 192
}