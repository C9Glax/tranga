using System.Threading.Channels;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

internal sealed class TaskQueue
{
    private readonly Channel<TaskBase> _queue = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<TaskBase>()
    {
        Comparer = new TaskBaseComparer() 
    });

    internal ValueTask AddTaskToQueue(TaskBase task, CancellationToken ct) => _queue.Writer.WriteAsync(task, ct);

    internal async Task<TaskBase?> GetNextTask(CancellationToken ct)
    {
        if (_queue.Reader.Count < 1) return null;
        return await _queue.Reader.ReadAsync(ct);
    }
    
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