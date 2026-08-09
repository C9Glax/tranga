using Microsoft.Extensions.Logging;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class TaskCapturingLoggerTests
{
    [Fact]
    public void Log_ForwardsToInnerLogger()
    {
        RecordingLogger inner = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TaskCapturingLogger logger = new(inner, task);

        logger.Log(LogLevel.Information, default, "hello", null, (s, _) => s);

        Assert.Single(inner.Entries);
        Assert.Equal(LogLevel.Information, inner.Entries[0].Level);
        Assert.Equal("hello", inner.Entries[0].Message);
    }

    [Fact]
    public void Log_AppendsToTaskLogEntries()
    {
        RecordingLogger inner = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TaskCapturingLogger logger = new(inner, task);

        logger.Log(LogLevel.Warning, default, "state", null, (_, _) => "message text");

        Services.Tasks.TaskTypes.TaskLogEntry entry = Assert.Single(task.LogEntries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("message text", entry.Message);
    }

    [Fact]
    public void Log_WithException_AppendsExceptionDetailsToMessage()
    {
        RecordingLogger inner = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TaskCapturingLogger logger = new(inner, task);
        InvalidOperationException exception = new("boom");

        logger.Log(LogLevel.Error, default, "state", exception, (_, _) => "failed");

        Services.Tasks.TaskTypes.TaskLogEntry entry = Assert.Single(task.LogEntries);
        Assert.Contains("failed", entry.Message);
        Assert.Contains("boom", entry.Message);
    }

    [Fact]
    public void Log_TrimsOldestEntriesBeyondMaxLogEntries()
    {
        RecordingLogger inner = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TaskCapturingLogger logger = new(inner, task);

        for (int i = 0; i < Services.Tasks.TaskTypes.TaskBase.MaxLogEntries + 10; i++)
            logger.Log(LogLevel.Information, default, i, null, (s, _) => $"message {s}");

        Assert.Equal(Services.Tasks.TaskTypes.TaskBase.MaxLogEntries, task.LogEntries.Count);
        Assert.Contains(task.LogEntries, e => e.Message == $"message {Services.Tasks.TaskTypes.TaskBase.MaxLogEntries + 9}");
        Assert.DoesNotContain(task.LogEntries, e => e.Message == "message 0");
    }

    [Fact]
    public void BeginScope_And_IsEnabled_ForwardToInnerLogger()
    {
        RecordingLogger inner = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TaskCapturingLogger logger = new(inner, task);

        Assert.Equal(inner.IsEnabled(LogLevel.Debug), logger.IsEnabled(LogLevel.Debug));
        using IDisposable? scope = logger.BeginScope("scope");
        Assert.Equal(1, inner.BeginScopeCalls);
    }

    private sealed class RecordingLogger : ILogger
    {
        internal readonly List<(LogLevel Level, string Message)> Entries = new();
        internal int BeginScopeCalls;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            BeginScopeCalls++;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
