using Logging;
using Tranga.LibraryConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public class TBaseObject
{
    protected Logger? logger { get; init; }
    protected TrangaSettings settings { get; init; }
    protected HashSet<NotificationConnector> notificationConnectors { get; init; }
    protected HashSet<LibraryConnector> libraryConnectors { get; init; }

    public TBaseObject(TBaseObject clone)
    {
        this.logger = clone.logger;
        this.settings = clone.settings;
        this.notificationConnectors = clone.notificationConnectors;
        this.libraryConnectors = clone.libraryConnectors;
    }

    public TBaseObject(Logger? logger, TrangaSettings settings, HashSet<NotificationConnector> notificationConnectors, HashSet<LibraryConnector> libraryConnectors)
    {
        this.logger = logger;
        this.settings = settings;
        this.notificationConnectors = notificationConnectors;
        this.libraryConnectors = libraryConnectors;
    }

    protected void Log(string message)
    {
        logger?.WriteLine(this.GetType().Name, message);
    }

    protected void Log(string fStr, params object?[] replace)
    {
        Log(string.Format(fStr, replace));
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

    protected void SendNotification(string title, string message)
    {
        foreach(NotificationConnector nc in notificationConnectors)
            nc.SendNotification(title, message);
    }

    protected void UpdateLibraries()
    {
        foreach (LibraryConnector libraryConnector in libraryConnectors)
            libraryConnector.UpdateLibrary();
    }
}