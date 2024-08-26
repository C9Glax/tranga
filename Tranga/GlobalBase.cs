using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Logging;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.MangaConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public abstract class GlobalBase
{
    [JsonIgnore]
    public Logger? logger { get; init; }
    protected HashSet<NotificationConnector> notificationConnectors { get; init; }
    protected HashSet<LibraryConnector> libraryConnectors { get; init; }
    private Dictionary<string, Manga> cachedPublications { get; init; }
    protected HashSet<MangaConnector> _connectors;
    public static readonly NumberFormatInfo numberFormatDecimalPoint = new (){ NumberDecimalSeparator = "." };
    protected static readonly Regex baseUrlRex = new(@"https?:\/\/[0-9A-z\.-]+(:[0-9]+)?");

    protected GlobalBase(GlobalBase clone)
    {
        this.logger = clone.logger;
        this.notificationConnectors = clone.notificationConnectors;
        this.libraryConnectors = clone.libraryConnectors;
        this.cachedPublications = clone.cachedPublications;
        this._connectors = clone._connectors;
    }

    protected GlobalBase(Logger? logger)
    {
        this.logger = logger;
        this.notificationConnectors = TrangaSettings.LoadNotificationConnectors(this);
        this.libraryConnectors = TrangaSettings.LoadLibraryConnectors(this);
        this.cachedPublications = new();
        this._connectors = new();
    }

    protected Manga? GetCachedManga(string internalId)
    {
        return cachedPublications.TryGetValue(internalId, out Manga manga) switch
        {
            true => manga,
            _ => null
        };
    }

    protected IEnumerable<Manga> GetAllCachedManga() => cachedPublications.Values;

    protected void AddMangaToCache(Manga manga)
    {
        if (!cachedPublications.TryAdd(manga.internalId, manga))
        {
            Log($"Overwriting Manga {manga.internalId}");
            cachedPublications[manga.internalId] = manga;
        }
        ExportManga();
    }
    
    protected void RemoveMangaFromCache(Manga manga) => RemoveMangaFromCache(manga.internalId);

    protected void RemoveMangaFromCache(string internalId)
    {
        cachedPublications.Remove(internalId);
        ExportManga();
    }

    internal void ImportManga()
    {
        string folder = TrangaSettings.mangaCacheFolderPath;
        Directory.CreateDirectory(folder);

        foreach (FileInfo fileInfo in new DirectoryInfo(folder).GetFiles())
        {
            string content = File.ReadAllText(fileInfo.FullName);
            try
            {
                Manga m = JsonConvert.DeserializeObject<Manga>(content, new MangaConnectorJsonConverter(this, _connectors));
                this.cachedPublications.TryAdd(m.internalId, m);
            }
            catch (JsonException e)
            {
                Log($"Error parsing Manga {fileInfo.Name}:\n{e.Message}");
            }
        }
        
    }

    private void ExportManga()
    {
        string folder = TrangaSettings.mangaCacheFolderPath;
        Directory.CreateDirectory(folder);
        foreach (Manga manga in cachedPublications.Values)
        {
            string content = JsonConvert.SerializeObject(manga, Formatting.Indented);
            string filePath = Path.Combine(folder, $"{manga.internalId}.json");
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        foreach (FileInfo fileInfo in new DirectoryInfo(folder).GetFiles())
        {
            if(!cachedPublications.Keys.Any(key => fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf('.')).Equals(key)))
                fileInfo.Delete();
        }
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
        Log($"Adding {notificationConnector}");
        notificationConnectors.RemoveWhere(nc => nc.notificationConnectorType == notificationConnector.notificationConnectorType);
        notificationConnectors.Add(notificationConnector);
        
        while(IsFileInUse(TrangaSettings.notificationConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting notificationConnectors");
        File.WriteAllText(TrangaSettings.notificationConnectorsFilePath, JsonConvert.SerializeObject(notificationConnectors));
    }

    protected void DeleteNotificationConnector(NotificationConnector.NotificationConnectorType notificationConnectorType)
    {
        Log($"Removing {notificationConnectorType}");
        notificationConnectors.RemoveWhere(nc => nc.notificationConnectorType == notificationConnectorType);
        while(IsFileInUse(TrangaSettings.notificationConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting notificationConnectors");
        File.WriteAllText(TrangaSettings.notificationConnectorsFilePath, JsonConvert.SerializeObject(notificationConnectors));
    }

    protected void UpdateLibraries()
    {
        foreach(LibraryConnector lc in libraryConnectors)
            lc.UpdateLibrary();
    }

    protected void AddLibraryConnector(LibraryConnector libraryConnector)
    {
        Log($"Adding {libraryConnector}");
        libraryConnectors.RemoveWhere(lc => lc.libraryType == libraryConnector.libraryType);
        libraryConnectors.Add(libraryConnector);
        
        while(IsFileInUse(TrangaSettings.libraryConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting libraryConnectors");
        File.WriteAllText(TrangaSettings.libraryConnectorsFilePath, JsonConvert.SerializeObject(libraryConnectors, Formatting.Indented));
    }

    protected void DeleteLibraryConnector(LibraryConnector.LibraryType libraryType)
    {
        Log($"Removing {libraryType}");
        libraryConnectors.RemoveWhere(lc => lc.libraryType == libraryType);
        while(IsFileInUse(TrangaSettings.libraryConnectorsFilePath))
            Thread.Sleep(100);
        Log("Exporting libraryConnectors");
        File.WriteAllText(TrangaSettings.libraryConnectorsFilePath, JsonConvert.SerializeObject(libraryConnectors, Formatting.Indented));
    }

    protected bool IsFileInUse(string filePath) => IsFileInUse(filePath, this.logger);

    public static bool IsFileInUse(string filePath, Logger? logger)
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
            logger?.WriteLine($"File is in use {filePath}");
            return true;
        }
    }
}