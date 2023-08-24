using System.Text;

namespace Logging;

public class Logger : TextWriter
{
    public override Encoding Encoding { get; }
    public enum LoggerType
    {
        FileLogger,
        ConsoleLogger
    }

    private readonly FileLogger? _fileLogger;
    private readonly FormattedConsoleLogger? _formattedConsoleLogger;
    private readonly MemoryLogger _memoryLogger;

    public Logger(LoggerType[] enabledLoggers, TextWriter? stdOut, Encoding? encoding, string? logFilePath)
    {
        this.Encoding = encoding ?? Encoding.ASCII;
        if (enabledLoggers.Contains(LoggerType.FileLogger) && logFilePath is not null)
            _fileLogger = new FileLogger(logFilePath, encoding);
        else if(enabledLoggers.Contains(LoggerType.FileLogger) && logFilePath is null)
        {
            _fileLogger = null;
            throw new ArgumentException($"logFilePath can not be null for LoggerType {LoggerType.FileLogger}");
        }

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

    public void WriteLine(string caller, string? value)
    {
        value = value is null ? Environment.NewLine : string.Concat(value, Environment.NewLine);

        Write(caller, value);
    }

    public void Write(string caller, string? value)
    {
        if (value is null)
            return;
        
        _fileLogger?.Write(caller, value);
        _formattedConsoleLogger?.Write(caller, value);
        _memoryLogger.Write(caller, value);
    }

    public string[] Tail(uint? lines)
    {
        return _memoryLogger.Tail(lines);
    }

    public string[] GetNewLines()
    {
        return _memoryLogger.GetNewLines();
    }
}