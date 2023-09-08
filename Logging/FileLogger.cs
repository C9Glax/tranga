using System.Text;

namespace Logging;

public class FileLogger : LoggerBase
{
    internal string logFilePath { get; }
    private const int MaxNumberOfLogFiles = 5;

    public FileLogger(string logFilePath, Encoding? encoding = null) : base (encoding)
    {
        this.logFilePath = logFilePath;

        DirectoryInfo dir = Directory.CreateDirectory(new FileInfo(logFilePath).DirectoryName!);
        
        //Remove oldest logfile if more than MaxNumberOfLogFiles
        for (int fileCount = dir.EnumerateFiles().Count(); fileCount > MaxNumberOfLogFiles - 1; fileCount--) //-1 because we create own logfile later
            File.Delete(dir.EnumerateFiles().MinBy(file => file.LastWriteTime)!.FullName);
    }
    
    protected override void Write(LogMessage logMessage)
    {
        try
        {
            File.AppendAllText(logFilePath, logMessage.formattedMessage);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}