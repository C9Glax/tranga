using System.Globalization;
using System.Text.RegularExpressions;
using GlaxLogger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public abstract class GlobalBase
{
    internal Logger? logger { get; init; }
    internal TrangaSettings settings { get; init; }
    internal HashSet<NotificationConnector> notificationConnectors { get; init; }
    internal HashSet<LibraryConnector> libraryConnectors { get; init; }
    internal List<Manga> cachedPublications { get; init; }
    internal static readonly NumberFormatInfo numberFormatDecimalPoint = new (){ NumberDecimalSeparator = "." };
    internal static readonly Regex baseUrlRex = new(@"https?:\/\/[0-9A-z\.-]+(:[0-9]+)?");

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
        this.notificationConnectors = settings.LoadNotificationConnectors(this);
        this.libraryConnectors = settings.LoadLibraryConnectors(this);
        this.cachedPublications = new();
    }

    internal void Log(string message)
    {
        Log(this.GetType().Name, message);
    }

    internal void Log(string objectType, string message)
    {
        logger?.LogInformation($"{objectType} | {message}");
    }

    internal void SendNotifications(string title, string text)
    {
        foreach (NotificationConnector nc in notificationConnectors)
            nc.SendNotification(title, text);
    }

    internal void AddNotificationConnector(NotificationConnector notificationConnector)
    {
        Log($"Adding {notificationConnector}");
        notificationConnectors.RemoveWhere(nc => nc.notificationConnectorType == notificationConnector.notificationConnectorType);
        notificationConnectors.Add(notificationConnector);
        
        while(IsFileInUse(settings.notificationConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting notificationConnectors");
        File.WriteAllText(settings.notificationConnectorsFilePath, JsonConvert.SerializeObject(notificationConnectors));
    }

    internal void DeleteNotificationConnector(NotificationConnector.NotificationConnectorType notificationConnectorType)
    {
        Log($"Removing {notificationConnectorType}");
        notificationConnectors.RemoveWhere(nc => nc.notificationConnectorType == notificationConnectorType);
        while(IsFileInUse(settings.notificationConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting notificationConnectors");
        File.WriteAllText(settings.notificationConnectorsFilePath, JsonConvert.SerializeObject(notificationConnectors));
    }

    internal void UpdateLibraries()
    {
        foreach(LibraryConnector lc in libraryConnectors)
            lc.UpdateLibrary();
    }

    internal void AddLibraryConnector(LibraryConnector libraryConnector)
    {
        Log($"Adding {libraryConnector}");
        libraryConnectors.RemoveWhere(lc => lc.libraryType == libraryConnector.libraryType);
        libraryConnectors.Add(libraryConnector);
        
        while(IsFileInUse(settings.libraryConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting libraryConnectors");
        File.WriteAllText(settings.libraryConnectorsFilePath, JsonConvert.SerializeObject(libraryConnectors));
    }

    internal void DeleteLibraryConnector(LibraryConnector.LibraryType libraryType)
    {
        Log($"Removing {libraryType}");
        libraryConnectors.RemoveWhere(lc => lc.libraryType == libraryType);
        while(IsFileInUse(settings.libraryConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting libraryConnectors");
        File.WriteAllText(settings.libraryConnectorsFilePath, JsonConvert.SerializeObject(libraryConnectors));
    }

    internal bool IsFileInUse(string filePath)
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