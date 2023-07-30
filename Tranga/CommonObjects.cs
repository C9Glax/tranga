using Logging;
using Newtonsoft.Json;
using Tranga.LibraryManagers;
using Tranga.NotificationManagers;

namespace Tranga;

public class CommonObjects
{
    public HashSet<LibraryManager> libraryManagers { get; init; }
    public HashSet<NotificationManager> notificationManagers { get; init; }
    public Logger? logger { get; set; }
    [JsonIgnore]private string settingsFilePath { get; init; }

    public CommonObjects(HashSet<LibraryManager>? libraryManagers, HashSet<NotificationManager>? notificationManagers, Logger? logger, string settingsFilePath)
    {
        this.libraryManagers = libraryManagers??new();
        this.notificationManagers = notificationManagers??new();
        this.logger = logger;
        this.settingsFilePath = settingsFilePath;
    }

    public static CommonObjects LoadSettings(string settingsFilePath, Logger? logger)
    {
        if (!File.Exists(settingsFilePath))
            return new CommonObjects(null, null, logger, settingsFilePath);

        string toRead = File.ReadAllText(settingsFilePath);
        TrangaSettings.SettingsJsonObject settings = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(
            toRead,
            new JsonSerializerSettings
            {
                Converters =
                {
                    new NotificationManager.NotificationManagerJsonConverter(),
                    new LibraryManager.LibraryManagerJsonConverter()
                }
            })!;
        
        if(settings.co is null)
            return new CommonObjects(null, null, logger, settingsFilePath);
        
        if (logger is not null)
        {
            foreach (LibraryManager lm in settings.co.libraryManagers)
                lm.AddLogger(logger);
            foreach(NotificationManager nm in settings.co.notificationManagers)
                nm.AddLogger(logger);
        }

        return settings.co;
    }
    
    public void ExportSettings()
    {
        TrangaSettings.SettingsJsonObject? settings = null;
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
            string toRead = File.ReadAllText(settingsFilePath);
            settings = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(toRead,
                new JsonSerializerSettings
                {
                    Converters =
                    {
                        new NotificationManager.NotificationManagerJsonConverter(),
                        new LibraryManager.LibraryManagerJsonConverter()
                    }
                });
        }
        settings = new TrangaSettings.SettingsJsonObject(settings?.ts, this);
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(settings));
    }

    public void UpdateSettings(TrangaSettings.UpdateField field, params string[] values)
    {
        switch (field)
        {
            case TrangaSettings.UpdateField.Komga:
                if (values.Length != 2)
                    return;
                libraryManagers.RemoveWhere(lm => lm.GetType() == typeof(Komga));
                libraryManagers.Add(new Komga(values[0], values[1], this.logger));
                break;
            case TrangaSettings.UpdateField.Kavita:
                if (values.Length != 3)
                    return;
                libraryManagers.RemoveWhere(lm => lm.GetType() == typeof(Kavita));
                libraryManagers.Add(new Kavita(values[0], values[1], values[2], this.logger));
                break;
            case TrangaSettings.UpdateField.Gotify:
                if (values.Length != 2)
                    return;
                notificationManagers.RemoveWhere(nm => nm.GetType() == typeof(Gotify));
                Gotify newGotify = new(values[0], values[1], this.logger);
                notificationManagers.Add(newGotify);
                newGotify.SendNotification("Success!", "Gotify was added to Tranga!");
                break;
            case TrangaSettings.UpdateField.LunaSea:
                if(values.Length != 1)
                    return;
                notificationManagers.RemoveWhere(nm => nm.GetType() == typeof(LunaSea));
                LunaSea newLunaSea = new(values[0], this.logger);
                notificationManagers.Add(newLunaSea);
                newLunaSea.SendNotification("Success!", "LunaSea was added to Tranga!");
                break;
        }
        ExportSettings();
    }
}