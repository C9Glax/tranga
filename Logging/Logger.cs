using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Logging;

public class Logger : ILogger
{
    private static readonly string LogDirectoryPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "/var/log/tranga-api"
        : Path.Join(Directory.GetCurrentDirectory(), "logs");
    public string? logFilePath => _fileLogger?.logFilePath;
    public enum LoggerType
    {
        FileLogger,
        ConsoleLogger
    }

    private readonly FileLogger? _fileLogger;
    private readonly FormattedConsoleLogger? _formattedConsoleLogger;
    private readonly MemoryLogger _memoryLogger;
    private readonly LogLevel _filterLevel;

    public Logger(LoggerType[] enabledLoggers, TextWriter? stdOut, Encoding? encoding, string? logFilePath, LogLevel logLevel = LogLevel.Debug)
    {
        this._filterLevel = logLevel;
        if(enabledLoggers.Contains(LoggerType.FileLogger) && (logFilePath is null || logFilePath == ""))
        {
            DateTime now = DateTime.Now;
            logFilePath = Path.Join(LogDirectoryPath,
                $"{now.ToShortDateString()}_{now.Hour}-{now.Minute}-{now.Second}.log");
            _fileLogger = new FileLogger(logFilePath, encoding);
        }else if (enabledLoggers.Contains(LoggerType.FileLogger) && logFilePath is not null)
            _fileLogger = new FileLogger(logFilePath, encoding);
        

        if (enabledLoggers.Contains(LoggerType.ConsoleLogger) && stdOut is not null)
        {
            _formattedConsoleLogger = new FormattedConsoleLogger(stdOut, encoding);
        }
        else if (enabledLoggers.Contains(LoggerType.ConsoleLogger) && stdOut is null)
        {
            _formattedConsoleLogger = null;
            throw new ArgumentException($"stdOut can not be null for LoggerType {LoggerType.ConsoleLogger}");
        }
        _memoryLogger = new MemoryLogger(encoding);
    }
    
    public string[] Tail(uint? lines)
    {
        return _memoryLogger.Tail(lines);
    }

    public string[] GetNewLines()
    {
        return _memoryLogger.GetNewLines();
    }

    public string[] GetLog()
    {
        return _memoryLogger.GetLogMessages();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        Type? test = new StackFrame(2).GetMethod()?.DeclaringType;
        if (!IsEnabled(logLevel))
            return;
        LogMessage message = new (DateTime.Now, test?.FullName ?? "", formatter.Invoke(state, exception));
        _fileLogger?.Write(message);
        _formattedConsoleLogger?.Write(message);
        _memoryLogger.Write(message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _filterLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
    {
        return null;
    }
}