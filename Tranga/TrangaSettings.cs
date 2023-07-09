using System.Text.RegularExpressions;
using Logging;
using Newtonsoft.Json;
using Tranga.LibraryManagers;
using Tranga.NotificationManagers;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; set; }
    public string workingDirectory { get; set; }
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public HashSet<LibraryManager> libraryManagers { get; }
    public HashSet<NotificationManager> notificationManagers { get; }

    public TrangaSettings(string downloadLocation, string workingDirectory, HashSet<LibraryManager>? libraryManagers,
        HashSet<NotificationManager>? notificationManagers)
    {
        if (downloadLocation.Length < 1 || workingDirectory.Length < 1)
            throw new ArgumentException("Download-location and working-directory paths can not be empty!");
        this.workingDirectory = workingDirectory;
        this.downloadLocation = downloadLocation;
        this.libraryManagers = libraryManagers??new();
        this.notificationManagers = notificationManagers??new();
    }

    public static TrangaSettings LoadSettings(string importFilePath, Logger? logger)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings(Path.Join(Directory.GetCurrentDirectory(), "Downloads"),
                Directory.GetCurrentDirectory(), new HashSet<LibraryManager>(), new HashSet<NotificationManager>());

        string toRead = File.ReadAllText(importFilePath);
        TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(toRead,
            new JsonSerializerSettings { Converters = { new NotificationManager.NotificationManagerJsonConverter(), new LibraryManager.LibraryManagerJsonConverter() } })!;
        if (logger is not null)
        {
            foreach (LibraryManager lm in settings.libraryManagers)
                lm.AddLogger(logger);
            foreach(NotificationManager nm in settings.notificationManagers)
                nm.AddLogger(logger);
        }

        return settings;
    }

    public void ExportSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            bool inUse = true;
            while (inUse)
            {
                try
                {
                    using FileStream stream = new (settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    stream.Close();
                    inUse = false;
                }
                catch (IOException)
                {
                    inUse = true;
                    Thread.Sleep(50);
                }
            }
        }
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(this));
    }

    public void UpdateSettings(UpdateField field, Logger? logger = null, params string[] values) 
    {
        switch (field)
        {
            case UpdateField.DownloadLocation:
                if (values.Length != 1)
                    return;
                this.downloadLocation = values[0];
                break;
            case UpdateField.Komga:
                if (values.Length != 2)
                    return;
                libraryManagers.RemoveWhere(lm => lm.GetType() == typeof(Komga));
                libraryManagers.Add(new Komga(values[0], values[1], logger));
                break;
            case UpdateField.Kavita:
                if (values.Length != 3)
                    return;
                libraryManagers.RemoveWhere(lm => lm.GetType() == typeof(Kavita));
                libraryManagers.Add(new Kavita(values[0], values[1], values[2], logger));
                break;
            case UpdateField.Gotify:
                if (values.Length != 2)
                    return;
                notificationManagers.RemoveWhere(nm => nm.GetType() == typeof(Gotify));
                Gotify newGotify = new(values[0], values[1], logger);
                notificationManagers.Add(newGotify);
                newGotify.SendNotification("Success!", "Gotify was added to Tranga!");
                break;
            case UpdateField.LunaSea:
                if(values.Length != 1)
                    return;
                notificationManagers.RemoveWhere(nm => nm.GetType() == typeof(LunaSea));
                LunaSea newLunaSea = new(values[0], logger);
                notificationManagers.Add(newLunaSea);
                newLunaSea.SendNotification("Success!", "LunaSea was added to Tranga!");
                break;
        }
        ExportSettings();
    }
    
    public enum UpdateField { DownloadLocation, Komga, Kavita, Gotify, LunaSea}
}