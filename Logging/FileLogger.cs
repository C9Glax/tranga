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
        try
        {
            File.AppendAllText(logFilePath, logMessage.ToString());
        }
        catch (Exception e)
        {
            stdOut?.WriteLine(e);
        }
    }
}