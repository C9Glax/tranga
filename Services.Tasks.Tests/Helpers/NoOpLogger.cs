using Microsoft.Extensions.Logging;

namespace Services.Tasks.Tests.Helpers;

internal sealed class NoOpLogger : ILogger
{
    public static NoOpLogger Instance { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class NoOpLogger<T> : ILogger<T>
{
    public static NoOpLogger<T> Instance { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpLogger.Instance.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}

internal sealed class NoOpLoggerFactory : ILoggerFactory
{
    public static NoOpLoggerFactory Instance { get; } = new();

    public ILogger CreateLogger(string categoryName) => NoOpLogger.Instance;

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
