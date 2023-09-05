using System.Text;

namespace Logging;

public class FileLogger : LoggerBase
{
    internal string logFilePath { get; }
    private const int MaxNumberOfLogFiles = 5;

    public FileLogger(string logFilePath, Encoding? encoding = null) : base (encoding)
    {
        this.logFilePath = logFilePath;
        
        //Remove oldest logfile if more than MaxNumberOfLogFiles
        string parentFolderPath = Path.GetDirectoryName(logFilePath)!;
        for (int fileCount = new DirectoryInfo(parentFolderPath).EnumerateFiles().Count(); fileCount > MaxNumberOfLogFiles - 1; fileCount--) //-1 because we create own logfile later
            File.Delete(new DirectoryInfo(parentFolderPath).EnumerateFiles().MinBy(file => file.LastWriteTime)!.FullName);
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