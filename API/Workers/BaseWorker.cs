using API.Schema;
using log4net;

namespace API.Workers;

public abstract class BaseWorker : Identifiable
{
    public BaseWorker[] DependsOn { get; init; }
    public IEnumerable<BaseWorker> AllDependencies => DependsOn.Select(d => d.AllDependencies).SelectMany(x => x);
    public IEnumerable<BaseWorker> DependenciesAndSelf => AllDependencies.Append(this);
    public IEnumerable<BaseWorker> MissingDependencies => DependsOn.Where(d => d.State < WorkerExecutionState.Completed);
    public bool DependenciesFulfilled => DependsOn.All(d => d.State >= WorkerExecutionState.Completed);
    internal WorkerExecutionState State { get; set; }
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromMinutes(10));
    protected ILog Log { get; init; }
    public void Cancel() => CancellationTokenSource.Cancel();
    protected void Fail() => this.State = WorkerExecutionState.Failed;

    public BaseWorker(IEnumerable<BaseWorker>? dependsOn = null)
    {
        this.DependsOn = dependsOn?.ToArray() ?? [];
        this.Log = LogManager.GetLogger(GetType());
    }

    public Task<BaseWorker[]> DoWork()
    {
        this.State = WorkerExecutionState.Waiting;
        
        BaseWorker[] missingDependenciesThatNeedStarting = MissingDependencies.Where(d => d.State < WorkerExecutionState.Waiting).ToArray();
        if(missingDependenciesThatNeedStarting.Any())
            return new Task<BaseWorker[]>(() => missingDependenciesThatNeedStarting);

        if (MissingDependencies.Any())
            return new Task<BaseWorker[]>(WaitForDependencies);
        
        Task<BaseWorker[]> task = new (DoWorkInternal, CancellationTokenSource.Token);
        task.GetAwaiter().OnCompleted(() => this.State = WorkerExecutionState.Completed);
        task.Start();
        this.State = WorkerExecutionState.Running;
        return task;
    }
    
    protected abstract BaseWorker[] DoWorkInternal();

    private BaseWorker[] WaitForDependencies()
    {
        while (CancellationTokenSource.IsCancellationRequested == false && MissingDependencies.Any())
        {
            Thread.Sleep(TrangaSettings.workCycleTimeout);  
        }
        return [this];
    }
}

public enum WorkerExecutionState
{
    Failed = 0,
    Created = 64,
    Waiting = 96,
    Running = 128,
    Completed = 192
}