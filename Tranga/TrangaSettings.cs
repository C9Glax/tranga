using Logging;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; private set; }
    public string workingDirectory { get; init; }
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public ushort? version { get; set; }

    public TrangaSettings(string? downloadLocation = null, string? workingDirectory = null)
    {
        downloadLocation ??= Path.Join(Directory.GetCurrentDirectory(), "Downloads");
        workingDirectory ??= Directory.GetCurrentDirectory();
        if (downloadLocation.Length < 1 || workingDirectory.Length < 1)
            throw new ArgumentException("Download-location and working-directory paths can not be empty!");
        this.workingDirectory = workingDirectory;
        this.downloadLocation = downloadLocation;
    }

    public static TrangaSettings LoadSettings(string importFilePath, Logger? logger)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings();

        string toRead = File.ReadAllText(importFilePath);
        TrangaSettings? settings = JsonConvert.DeserializeObject<TrangaSettings>(File.ReadAllText(importFilePath),
            new JsonSerializerSettings
            {
                Converters =
                {
                    new NotificationManagerJsonConverter(),
                    new LibraryManagerJsonConverter()
                }
            });
        return settings ?? new TrangaSettings();

    }

    public void ExportSettings()
    {
        while (File.Exists(settingsFilePath))
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
                    Thread.Sleep(100);
                }
            }
        }
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(this));
    }
}