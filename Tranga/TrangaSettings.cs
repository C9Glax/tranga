using Newtonsoft.Json;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; set; }
    public string workingDirectory { get; set; }
    public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    public string knownPublicationsPath => Path.Join(workingDirectory, "knownPublications.json");
    public Komga? komga { get; set; }

    public TrangaSettings(string downloadLocation, string? workingDirectory, Komga? komga)
    {
        this.workingDirectory = workingDirectory ??
                                Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tranga");
        this.downloadLocation = downloadLocation;
        this.komga = komga;
    }

    public static TrangaSettings LoadSettings(string importFilePath)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings(Directory.GetCurrentDirectory(), null, null);

        string toRead = File.ReadAllText(importFilePath);
        TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(toRead)!;

        return settings;
    }
}