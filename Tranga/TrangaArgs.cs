using Logging;
using GlaxArguments;

namespace Tranga;

public partial class Tranga : GlobalBase
{

    public static void Main(string[] args)
    {
        Argument downloadLocation = new (new[] { "-d", "--downloadLocation" }, 1, "Directory to which downloaded Manga are saved");
        Argument workingDirectory = new (new[] { "-w", "--workingDirectory" }, 1, "Directory in which application-data is saved");
        Argument consoleLogger = new (new []{"-c", "--consoleLogger"}, 0, "Enables the consoleLogger");
        Argument fileLogger = new (new []{"-f", "--fileLogger"}, 0, "Enables the fileLogger");
        Argument fPath = new (new []{"-l", "--fPath"}, 1, "Log Folder Path");
        
        Argument[] arguments = new[]
        {
            downloadLocation,
            workingDirectory,
            consoleLogger,
            fileLogger,
            fPath
        };
        ArgumentFetcher fetcher = new (arguments);
        Dictionary<Argument, string[]> fetched = fetcher.Fetch(args);

        string? directoryPath = fetched.TryGetValue(fPath, out string[]? path) ? path[0] : null;
        if (directoryPath is not null && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        List<Logger.LoggerType> enabledLoggers = new();
        if(fetched.ContainsKey(consoleLogger))
            enabledLoggers.Add(Logger.LoggerType.ConsoleLogger);
        if (fetched.ContainsKey(fileLogger))
            enabledLoggers.Add(Logger.LoggerType.FileLogger);
        Logger logger = new(enabledLoggers.ToArray(), Console.Out, Console.OutputEncoding, directoryPath);

        bool dlp = fetched.TryGetValue(downloadLocation, out string[]? downloadLocationPath);
        bool wdp = fetched.TryGetValue(workingDirectory, out string[]? workingDirectoryPath);

        if (wdp)
            TrangaSettings.LoadFromWorkingDirectory(workingDirectoryPath![0]);
        if(dlp)
            TrangaSettings.CreateOrUpdate(downloadDirectory: downloadLocationPath![0]);

        Tranga _ = new (logger);
    }
}