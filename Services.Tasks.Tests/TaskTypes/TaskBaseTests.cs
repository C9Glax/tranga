using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tests.TaskTypes;

public class TaskBaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesFreshScopePerRun()
    {
        TestScopedService firstScopedService = new("first");
        TestScopedService secondScopedService = new("second");
        TestTask task = new();
        TestServiceScope firstScope = new(firstScopedService);
        TestServiceScope secondScope = new(secondScopedService);

        await task.ExecuteAsync(firstScope, NoOpLogger.Instance, CancellationToken.None);
        await task.ExecuteAsync(secondScope, NoOpLogger.Instance, CancellationToken.None);

        Assert.Collection(task.RefreshedScopes,
            scope => Assert.Same(firstScope, scope),
            scope => Assert.Same(secondScope, scope));
        Assert.Equal("first", task.CapturedValueAfterFirstRun);
        Assert.Equal("second", task.CapturedValueAfterSecondRun);
        Assert.NotNull(task.LastRun);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesLastRunOnSuccess()
    {
        TestTask task = new();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        await task.ExecuteAsync(new TestServiceScope(new TestScopedService("value")), NoOpLogger.Instance, CancellationToken.None);

        Assert.NotNull(task.LastRun);
        Assert.True(task.LastRun >= before);
    }

    private sealed class TestTask() : TaskBase(TaskType.RunOnceTask, Guid.NewGuid())
    {
        public List<IServiceScope> RefreshedScopes { get; } = [];

        public string? CapturedValueAfterFirstRun { get; private set; }

        public string? CapturedValueAfterSecondRun { get; private set; }

        private int _runCount;

        protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
        {
            _runCount++;
            string value = scope.ServiceProvider.GetRequiredService<TestScopedService>().Value;

            if (_runCount == 1)
                CapturedValueAfterFirstRun = value;
            else if (_runCount == 2)
                CapturedValueAfterSecondRun = value;

            return Task.CompletedTask;
        }

        protected override void RefreshScope(IServiceScope scope)
        {
            RefreshedScopes.Add(scope);
        }
    }

    private sealed class TestServiceScope(TestScopedService scopedService) : IServiceScope, IServiceProvider
    {
        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType) => serviceType == typeof(TestScopedService) ? scopedService : null;

        public void Dispose()
        {
        }
    }

    private sealed record TestScopedService(string Value);

    private sealed class NoOpLogger : ILogger
    {
        public static NoOpLogger Instance { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

