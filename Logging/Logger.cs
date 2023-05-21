using System.Net.Mime;
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

    private FileLogger? _fileLogger;
    private FormattedConsoleLogger? _formattedConsoleLogger;
    private MemoryLogger _memoryLogger;
    private TextWriter? stdOut;

    public Logger(LoggerType[] enabledLoggers, TextWriter? stdOut, Encoding? encoding, string? logFilePath)
    {
        this.Encoding = encoding ?? Encoding.ASCII;
        this.stdOut = stdOut ?? null;
        if (enabledLoggers.Contains(LoggerType.FileLogger) && logFilePath is not null)
            _fileLogger = new FileLogger(logFilePath, null, encoding);
        else
        {
            _fileLogger = null;
            throw new ArgumentException($"logFilePath can not be null for LoggerType {LoggerType.FileLogger}");
        }
        _formattedConsoleLogger = enabledLoggers.Contains(LoggerType.ConsoleLogger) ? new FormattedConsoleLogger(null, encoding) : null;
        _memoryLogger = new MemoryLogger(null, encoding);
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
        stdOut?.Write(value);
    }

    public string[] Tail(uint? lines)
    {
        return _memoryLogger.Tail(lines);
    }
}