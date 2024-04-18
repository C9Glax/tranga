using Logging;

namespace Tranga;

public partial class Tranga : GlobalBase
{

    public static void Main(string[] args)
    {
        Console.WriteLine(string.Join(' ', args));
        string[]? help = GetArg(args, ArgEnum.Help);
        if (help is not null)
        {
            PrintHelp();
            return;
        }
        
        string[]? consoleLogger = GetArg(args, ArgEnum.ConsoleLogger);
        string[]? fileLogger = GetArg(args, ArgEnum.FileLogger);
        string? directoryPath = GetArg(args, ArgEnum.FileLoggerPath)?[0];
        if (directoryPath is not null && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        
        List<Logger.LoggerType> enabledLoggers = new();
        if(consoleLogger is not null)
            enabledLoggers.Add(Logger.LoggerType.ConsoleLogger);
        if (fileLogger is not null)
            enabledLoggers.Add(Logger.LoggerType.FileLogger);
        Logger logger = new(enabledLoggers.ToArray(), Console.Out, Console.OutputEncoding, directoryPath);

        TrangaSettings? settings = null;
        string[]? downloadLocationPath = GetArg(args, ArgEnum.DownloadLocation);
        string[]? workingDirectory = GetArg(args, ArgEnum.WorkingDirectory);

        if (downloadLocationPath is not null && workingDirectory is not null)
        {
            settings = new TrangaSettings(downloadLocationPath[0], workingDirectory[0]);
        }else if (downloadLocationPath is not null)
        {
            if (settings is null)
                settings = new TrangaSettings(downloadLocation: downloadLocationPath[0]);
            else
                settings = new TrangaSettings(downloadLocation: downloadLocationPath[0], settings.workingDirectory);
        }else if (workingDirectory is not null)
        {
            if (settings is null)
                settings = new TrangaSettings(downloadLocation: workingDirectory[0]);
            else
                settings = new TrangaSettings(settings.downloadLocation, workingDirectory[0]);
        }
        else
        {
            settings = new TrangaSettings();
        }
        
        Directory.CreateDirectory(settings.downloadLocation);//TODO validate path
        Directory.CreateDirectory(settings.workingDirectory);//TODO validate path

        Tranga _ = new (logger, settings);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Tranga-Help:");
        foreach (Argument argument in Arguments.Values)
        {
            foreach(string name in argument.names)
                Console.Write("{0} ", name);
            if(argument.parameterCount > 0)
                Console.Write($"<{argument.parameterCount}>");
            Console.Write("\r\n  {0}\r\n", argument.helpText);
        }
    }
    
    /// <summary>
    /// Returns an array containing the parameters for the argument.
    /// </summary>
    /// <param name="args">List of argument-strings</param>
    /// <param name="arg">Requested parameter</param>
    /// <returns>
    /// If there are no parameters for an argument, returns an empty array.
    /// If the argument is not found returns null.
    /// </returns>
    private static string[]? GetArg(string[] args, ArgEnum arg)
    {
        List<string> argsList = args.ToList();
        List<string> ret = new();
        foreach (string name in Arguments[arg].names)
        {
            int argIndex = argsList.IndexOf(name);
            if (argIndex != -1)
            {
                if (Arguments[arg].parameterCount == 0)
                    return ret.ToArray();
                for (int parameterIndex = 1; parameterIndex <= Arguments[arg].parameterCount; parameterIndex++)
                {
                    if(argIndex + parameterIndex >= argsList.Count || args[argIndex + parameterIndex].Contains('-'))//End of arguments, or no parameter provided, when one is required
                        Console.WriteLine($"No parameter provided for argument {name}. -h for help.");
                    ret.Add(args[argIndex + parameterIndex]);
                }
            }
        }
        return ret.Any() ? ret.ToArray() : null;
    }

    private static readonly Dictionary<ArgEnum, Argument> Arguments = new()
    {
        { ArgEnum.DownloadLocation, new(new []{"-d", "--downloadLocation"}, 1, "Directory to which downloaded Manga are saved") },
        { ArgEnum.WorkingDirectory, new(new []{"-w", "--workingDirectory"}, 1, "Directory in which application-data is saved") },
        { ArgEnum.ConsoleLogger, new(new []{"-c", "--consoleLogger"}, 0, "Enables the consoleLogger") },
        { ArgEnum.FileLogger, new(new []{"-f", "--fileLogger"}, 0, "Enables the fileLogger") },
        { ArgEnum.FileLoggerPath, new (new []{"-l", "--fPath"}, 1, "Log Folder Path" ) },
        { ArgEnum.Help, new(new []{"-h", "--help"}, 0, "Print this") }
        //{ ArgEnum., new(new []{""}, 1, "") }
    };

    internal enum ArgEnum
    {
        TrangaSettings,
        DownloadLocation,
        WorkingDirectory,
        ConsoleLogger,
        FileLogger,
        FileLoggerPath,
        Help
    }

    private struct Argument
    {
        public string[] names { get; }
        public byte parameterCount { get; }
        public string helpText { get; }

        public Argument(string[] names, byte parameterCount, string helpText)
        {
            this.names = names;
            this.parameterCount = parameterCount;
            this.helpText = helpText;
        }
    }
}