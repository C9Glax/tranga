using System.Threading.Channels;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

internal sealed class TaskQueue
{
    private readonly HashSet<Guid> _tasksInQueue = [];
    
    private readonly Channel<TaskBase> _queue = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<TaskBase>()
    {
        Comparer = new TaskBaseComparer() 
    });

    internal ValueTask AddTaskToQueue(TaskBase task, CancellationToken ct)
    {
        if (!_tasksInQueue.Add(task.TaskId)) return ValueTask.CompletedTask;
        task.Status = TaskState.Queued;
        return _queue.Writer.WriteAsync(task, ct);
    }

    internal async Task<TaskBase?> GetNextTask(CancellationToken ct)
    {
        if (_queue.Reader.Count < 1) return null;
        TaskBase value = await _queue.Reader.ReadAsync(ct);
        _tasksInQueue.Remove(value.TaskId);
        return value;
    }

    internal bool ContainsTask(Guid taskId) => _tasksInQueue.Contains(taskId);

    /// <summary>
    /// Number of Tasks currently waiting in the ready queue for a worker to pick up.
    /// </summary>
    internal int ReadyCount => _queue.Reader.Count;
    
    private class TaskBaseComparer : IComparer<TaskBase>
    {
        public int Compare(TaskBase? x, TaskBase? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (y is null) return 1;
            if (x is null) return -1;
            return x.Priority.CompareTo(y.Priority);
        }
    }
}