using System.Runtime.InteropServices;
using System.Text;

namespace Logging;

public class Logger : TextWriter
{
    private static readonly string LogDirectoryPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? "/var/log/tranga-api"
        : Path.Join(Directory.GetCurrentDirectory(), "logs");
    public string? logFilePath => _fileLogger?.logFilePath;
    public override Encoding Encoding { get; }
    public enum LoggerType
    {
        FileLogger,
        ConsoleLogger
    }

    private readonly FileLogger? _fileLogger;
    private readonly FormattedConsoleLogger? _formattedConsoleLogger;
    private readonly MemoryLogger _memoryLogger;

    public Logger(LoggerType[] enabledLoggers, TextWriter? stdOut, Encoding? encoding, string? logFolderPath)
    {
        this.Encoding = encoding ?? Encoding.UTF8;
        DateTime now = DateTime.Now;
        if(enabledLoggers.Contains(LoggerType.FileLogger) && (logFolderPath is null || logFolderPath == ""))
        {
            string filePath = Path.Join(LogDirectoryPath,
                $"{now.ToShortDateString()}_{now.Hour}-{now.Minute}-{now.Second}.log");
            _fileLogger = new FileLogger(filePath, encoding);
        }else if (enabledLoggers.Contains(LoggerType.FileLogger) && logFolderPath is not null)
            _fileLogger = new FileLogger(Path.Join(logFolderPath, $"{now.ToShortDateString()}_{now.Hour}-{now.Minute}-{now.Second}.log") , encoding);
        

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

    public string[] GetLog()
    {
        return _memoryLogger.GetLogMessages();
    }
}