using Newtonsoft.Json;
using Tranga.LibraryManagers;
using Tranga.NotificationManagers;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; private set; }
    public string workingDirectory { get; init; }
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public ushort? version { get; set; }

    public TrangaSettings(string downloadLocation, string workingDirectory)
    {
        if (downloadLocation.Length < 1 || workingDirectory.Length < 1)
            throw new ArgumentException("Download-location and working-directory paths can not be empty!");
        this.workingDirectory = workingDirectory;
        this.downloadLocation = downloadLocation;
    }

    public static TrangaSettings LoadSettings(string importFilePath)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings(Path.Join(Directory.GetCurrentDirectory(), "Downloads"), Directory.GetCurrentDirectory());

        string toRead = File.ReadAllText(importFilePath);
        SettingsJsonObject settings = JsonConvert.DeserializeObject<SettingsJsonObject>(toRead,
            new JsonSerializerSettings { Converters = { new NotificationManager.NotificationManagerJsonConverter(), new LibraryManager.LibraryManagerJsonConverter() } })!;
        return settings.ts ?? new TrangaSettings(Path.Join(Directory.GetCurrentDirectory(), "Downloads"), Directory.GetCurrentDirectory());

    }

    public void ExportSettings()
    {
        SettingsJsonObject? settings = null;
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
            settings = JsonConvert.DeserializeObject<SettingsJsonObject>(toRead,
                new JsonSerializerSettings
                {
                    Converters =
                    {
                        new NotificationManager.NotificationManagerJsonConverter(),
                        new LibraryManager.LibraryManagerJsonConverter()
                    }
                });
        }
        settings = new SettingsJsonObject(this, settings?.co);
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(settings));
    }

    public void UpdateSettings(UpdateField field, params string[] values) 
    {
        switch (field)
        {
            case UpdateField.DownloadLocation:
                if (values.Length != 1)
                    return;
                this.downloadLocation = values[0];
                break;
        }
        ExportSettings();
    }
    
    public enum UpdateField { DownloadLocation, Komga, Kavita, Gotify, LunaSea}

    internal class SettingsJsonObject
    {
        public TrangaSettings? ts { get; }
        public CommonObjects? co { get; }

        public SettingsJsonObject(TrangaSettings? ts, CommonObjects? co)
        {
            this.ts = ts;
            this.co = co;
        }
    }
}