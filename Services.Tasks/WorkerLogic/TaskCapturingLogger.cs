using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// An <see cref="ILogger"/> decorator that forwards every call to <paramref name="inner"/> unchanged, and also
/// mirrors it into <paramref name="task"/>'s <see cref="TaskBase.LogEntries"/> so it can be queried per Task later.
/// </summary>
internal sealed class TaskCapturingLogger(ILogger inner, TaskBase task) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        inner.Log(logLevel, eventId, state, exception, formatter);
        string message = formatter(state, exception);
        task.AppendLog(logLevel, exception is null ? message : $"{message}\n{exception}");
    }
}
