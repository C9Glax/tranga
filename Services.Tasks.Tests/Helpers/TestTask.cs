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

/// <summary>
/// A <see cref="RunOnceTask"/> that implements <see cref="IMangaTask"/> with a settable <see cref="MangaId"/> and
/// <see cref="DependsOnTaskTypeIds"/>, for exercising <see cref="Services.Tasks.WorkerLogic.TaskDependencyResolver"/>.
/// </summary>
internal sealed class TestMangaRunOnceTask(Guid mangaId, Guid? taskTypeId = null, IReadOnlyCollection<Guid>? dependsOn = null)
    : RunOnceTask(taskTypeId ?? Guid.CreateVersion7()), IMangaTask
{
    public Guid MangaId { get; init; } = mangaId;

    internal override IReadOnlyCollection<Guid> DependsOnTaskTypeIds { get; } = dependsOn ?? [];

    protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken) => Task.CompletedTask;

    protected override void RefreshScope(IServiceScope scope)
    {
    }
}

/// <summary>
/// A <see cref="RunOnceTask"/> whose <see cref="RunAsync"/> always throws - used to verify failure handling
/// (e.g. <see cref="HasRun"/> still being set, <see cref="TaskBase.Status"/> becoming <see cref="TaskState.Failed"/>).
/// </summary>
internal sealed class ThrowingTestRunOnceTask(Guid? taskTypeId = null) : RunOnceTask(taskTypeId ?? Guid.CreateVersion7())
{
    protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken) =>
        throw new InvalidOperationException("boom");

    protected override void RefreshScope(IServiceScope scope)
    {
    }
}