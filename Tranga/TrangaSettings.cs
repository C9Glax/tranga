using Newtonsoft.Json;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; set; }
    public string workingDirectory { get; set; }
    [JsonIgnore]public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore]public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore]public string knownPublicationsPath => Path.Join(workingDirectory, "knownPublications.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public Komga? komga { get; set; }

    public TrangaSettings(string downloadLocation, string workingDirectory, Komga? komga)
    {
        this.workingDirectory = workingDirectory;
        this.downloadLocation = downloadLocation;
        this.komga = komga;
    }

    public static TrangaSettings LoadSettings(string importFilePath)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings(Path.Join(Directory.GetCurrentDirectory(), "Downloads"), Directory.GetCurrentDirectory(), null);

        string toRead = File.ReadAllText(importFilePath);
        TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(toRead)!;

        return settings;
    }
}