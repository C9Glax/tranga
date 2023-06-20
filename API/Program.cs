using System.Runtime.InteropServices;
using Logging;
using Tranga;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        string applicationFolderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tranga-API");
        string downloadFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(applicationFolderPath, "Manga");
        string logsFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/var/logs/Tranga" : Path.Join(applicationFolderPath, "logs");
        string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
        string settingsFilePath = Path.Join(applicationFolderPath, "settings.json");

        Directory.CreateDirectory(logsFolderPath);
        Logger logger = new(new[] { Logger.LoggerType.FileLogger, Logger.LoggerType.ConsoleLogger }, Console.Out, Console.Out.Encoding, logFilePath);

        logger.WriteLine("Tranga", "Loading settings.");

        TrangaSettings settings;
        if (File.Exists(settingsFilePath))
            settings = TrangaSettings.LoadSettings(settingsFilePath, logger);
        else
            settings = new TrangaSettings(downloadFolderPath, applicationFolderPath, new HashSet<LibraryManager>(), new HashSet<NotificationManager>());

        Directory.CreateDirectory(settings.workingDirectory);
        Directory.CreateDirectory(settings.downloadLocation);
        Directory.CreateDirectory(settings.coverImageCache);

        logger.WriteLine("Tranga",$"Application-Folder: {settings.workingDirectory}");
        logger.WriteLine("Tranga",$"Settings-File-Path: {settings.settingsFilePath}");
        logger.WriteLine("Tranga",$"Download-Folder-Path: {settings.downloadLocation}");
        logger.WriteLine("Tranga",$"Logfile-Path: {logFilePath}");
        logger.WriteLine("Tranga",$"Image-Cache-Path: {settings.coverImageCache}");

        logger.WriteLine("Tranga", "Loading Taskmanager.");
        TaskManager taskManager = new (settings, logger);
        
        Server server = new (6531, taskManager, logger);
        foreach(NotificationManager nm in taskManager.settings.notificationManagers)
            nm.SendNotification("Tranga-API", "Started Tranga-API");
    }
}

