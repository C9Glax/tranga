using System.Text;
using System.Text.Json.Serialization;

namespace Logging;

public class FileLogger : LoggerBase
{
    private string logFilePath { get; }

    public FileLogger(string logFilePath, TextWriter? stdOut, Encoding? encoding = null) : base (stdOut, encoding)
    {
        this.logFilePath = logFilePath;
    }
    
    protected override void Write(LogMessage logMessage)
    {
        File.AppendAllText(logFilePath, logMessage.ToString());
    }
}