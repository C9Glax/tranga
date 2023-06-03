using Logging;
using Newtonsoft.Json;
using Tranga.LibraryManagers;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; set; }
    public string workingDirectory { get; set; }
    [JsonIgnore]public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore]public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore]public string knownPublicationsPath => Path.Join(workingDirectory, "knownPublications.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public readonly HashSet<LibraryManager> libraryManagers;

    public TrangaSettings(string downloadLocation, string workingDirectory, HashSet<LibraryManager> libraryManagers)
    {
        if (downloadLocation.Length < 1 || workingDirectory.Length < 1)
            throw new ArgumentException("Download-location and working-directory paths can not be empty!");
        this.workingDirectory = workingDirectory;
        this.downloadLocation = downloadLocation;
        this.libraryManagers = libraryManagers;
    }

    public static TrangaSettings LoadSettings(string importFilePath, Logger? logger)
    {
        if (!File.Exists(importFilePath))
            return new TrangaSettings(Path.Join(Directory.GetCurrentDirectory(), "Downloads"), Directory.GetCurrentDirectory(), new HashSet<LibraryManager>());

        string toRead = File.ReadAllText(importFilePath);
        TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(toRead, new JsonSerializerSettings() { Converters = { new LibraryManager.LibraryManagerJsonConverter()} })!;
        if(logger is not null)
            foreach(LibraryManager lm in settings.libraryManagers)
                lm.AddLogger(logger);

        return settings;
    }
}