using API.Schema;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API.Workers;

public abstract class BaseWorker : Identifiable
{
    /// <summary>
    /// Workers this Worker depends on being completed before running.
    /// </summary>
    private BaseWorker[] DependsOn { get; init; }
    /// <summary>
    /// Dependencies and dependencies of dependencies. See also <see cref="DependsOn"/>.
    /// </summary>
    internal IEnumerable<BaseWorker> AllDependencies => DependsOn.Select(d => d.AllDependencies).SelectMany(x => x);
    /// <summary>
    /// <see cref="AllDependencies"/> and Self.
    /// </summary>
    internal IEnumerable<BaseWorker> DependenciesAndSelf => AllDependencies.Append(this);
    /// <summary>
    /// <see cref="DependsOn"/> where <see cref="WorkerExecutionState"/> is less than Completed.
    /// </summary>
    internal IEnumerable<BaseWorker> MissingDependencies => DependsOn.Where(d => d.State < WorkerExecutionState.Completed);
    public bool AllDependenciesFulfilled => DependsOn.All(d => d.State >= WorkerExecutionState.Completed);
    internal WorkerExecutionState State { get; private set; }
    private CancellationTokenSource _cancellationTokenSource = new ();
    protected CancellationToken CancellationToken => _cancellationTokenSource.Token;
    protected ILog Log { get; init; }

    /// <summary>
    /// Stops worker, and marks as <see cref="WorkerExecutionState"/>.Cancelled
    /// </summary>
    public void Cancel()
    {
        Log.DebugFormat("Cancelled {0}", ToString());
        this.State = WorkerExecutionState.Cancelled;
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Stops worker, and marks as <see cref="WorkerExecutionState"/>.Failed
    /// </summary>
    protected void Fail()
    {
        Log.DebugFormat("Failed {0}", ToString());
        this.State = WorkerExecutionState.Failed;
        _cancellationTokenSource.Cancel();
    }

    protected BaseWorker(IEnumerable<BaseWorker>? dependsOn = null)
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
        try
        {
            // Start the worker
            Log.DebugFormat("Checking {0}", ToString());
            _cancellationTokenSource = new(Constants.WorkerTimeout);
            State = WorkerExecutionState.Waiting;
            
            // Wait for dependencies, start them if necessary
            BaseWorker[] missingDependenciesThatNeedStarting = MissingDependencies.Where(d => d.State < WorkerExecutionState.Waiting).ToArray();
            if(missingDependenciesThatNeedStarting.Any())
                return new (() => missingDependenciesThatNeedStarting);

            if (MissingDependencies.Any())
                return new (WaitForDependencies);
            
            // Run the actual work
            Log.InfoFormat("Running {0}", ToString());
            DateTime startTime = DateTime.UtcNow;
            State = WorkerExecutionState.Running;
            Task<BaseWorker[]> task = DoWorkInternal();
            task.GetAwaiter().OnCompleted(Finish(startTime, callback));
            return task;
        }
        catch (Exception e)
        { 
            Log.Error(e.ToString());
            return Task.FromException<BaseWorker[]>(e);
        }
    }

    private Action Finish(DateTime startTime, Action? callback = null) => () =>
    {
        DateTime endTime = DateTime.UtcNow;
        Log.InfoFormat("Completed {0}\n\t{1} ms", this, endTime.Subtract(startTime).TotalMilliseconds);
        this.State = WorkerExecutionState.Completed;
        if(this is IPeriodic periodic)
            periodic.LastExecution = DateTime.UtcNow;
        callback?.Invoke();
    };
    
    protected abstract Task<BaseWorker[]> DoWorkInternal();

    private BaseWorker[] WaitForDependencies()
    {
        Log.InfoFormat("Waiting for {0} Dependencies {1}:\n\t{2}", MissingDependencies.Count(), this, string.Join("\n\t", MissingDependencies.Select(d => d.ToString())));
        while (!_cancellationTokenSource.IsCancellationRequested && MissingDependencies.Any())
        {
            Thread.Sleep(Tranga.Settings.WorkCycleTimeoutMs);  
        }
        return [this];
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum WorkerExecutionState
{
    Failed = 0,
    Cancelled = 32,
    Created = 64,
    Waiting = 96,
    Running = 128,
    Completed = 192
}