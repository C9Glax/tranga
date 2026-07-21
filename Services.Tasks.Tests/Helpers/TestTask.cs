using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tests.Helpers;


internal abstract class TestTask
{

    internal static T Create<T>(int priority = 3, Guid? taskTypeId = null) where T : TaskBase
    {
        return typeof(T) switch
        {
            { } t when t == typeof(TestRunOnceTask) => new TestRunOnceTask(taskTypeId, priority) as T,
            { } t when t == typeof(TestPeriodicTask) => new TestPeriodicTask(taskTypeId, priority) as T,
            _ => throw new ArgumentException($"Unsupported task type: {typeof(T).Name}")
        } ?? throw new InvalidOperationException();
    }
}

internal class TestRunOnceTask : RunOnceTask
{
    public TestRunOnceTask(Guid? taskTypeId = null, int priority = 3) : base(taskTypeId ?? Guid.CreateVersion7())
    {
        Priority = priority;
    }

    protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        
    }
}

internal class TestPeriodicTask : PeriodicTask
{
    public TestPeriodicTask(Guid? taskTypeId = null, int priority = 3) : base(taskTypeId ?? Guid.CreateVersion7())
    {
        Priority = priority;
    }
    
    protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        
    }

    internal override TimeSpan Interval { get; init; }
}