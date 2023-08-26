using Logging;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public abstract class GlobalBase
{
    protected Logger? logger { get; init; }
    protected TrangaSettings settings { get; init; }
    protected HashSet<NotificationConnector> notificationConnectors { get; init; }
    protected HashSet<LibraryConnector> libraryConnectors { get; init; }
    protected List<Publication> cachedPublications { get; init; }

    protected GlobalBase(GlobalBase clone)
    {
        this.logger = clone.logger;
        this.settings = clone.settings;
        this.notificationConnectors = clone.notificationConnectors;
        this.libraryConnectors = clone.libraryConnectors;
        this.cachedPublications = clone.cachedPublications;
    }

    protected GlobalBase(Logger? logger, TrangaSettings settings)
    {
        this.logger = logger;
        this.settings = settings;
        this.notificationConnectors = settings.LoadNotificationConnectors();
        this.libraryConnectors = settings.LoadLibraryConnectors();
        this.cachedPublications = new();
    }

    protected void Log(string message)
    {
        logger?.WriteLine(this.GetType().Name, message);
    }

    protected void Log(string fStr, params object?[] replace)
    {
        Log(string.Format(fStr, replace));
    }

    protected void SendNotifications(string title, string text)
    {
        foreach (NotificationConnector nc in notificationConnectors)
            nc.SendNotification(title, text);
    }

    protected void AddNotificationConnector(NotificationConnector notificationConnector)
    {
        notificationConnectors.RemoveWhere(nc => nc.GetType() == notificationConnector.GetType());
        notificationConnectors.Add(notificationConnector);
        
        while(IsFileInUse(settings.notificationConnectorsFilePath))
            Thread.Sleep(100);
        File.WriteAllText(settings.notificationConnectorsFilePath, JsonConvert.SerializeObject(notificationConnectors));
    }

    protected void UpdateLibraries()
    {
        foreach(LibraryConnector lc in libraryConnectors)
            lc.UpdateLibrary();
    }

    protected void AddLibraryConnector(LibraryConnector libraryConnector)
    {
        libraryConnectors.RemoveWhere(lc => lc.GetType() == libraryConnector.GetType());
        libraryConnectors.Add(libraryConnector);
        
        while(IsFileInUse(settings.libraryConnectorsFilePath))
            Thread.Sleep(100);
        File.WriteAllText(settings.libraryConnectorsFilePath, JsonConvert.SerializeObject(libraryConnectors));
    }

    protected bool IsFileInUse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;
        try
        {
            using FileStream stream = new (filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
            return false;
        }
        catch (IOException)
        {
            Log($"File is in use {filePath}");
            return true;
        }
    }
}