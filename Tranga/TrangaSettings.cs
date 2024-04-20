using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.MangaConnectors;
using Tranga.NotificationConnectors;
using static System.IO.UnixFileMode;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; private set; }
    public string workingDirectory { get; private set; }
    public int apiPortNumber { get; init; }
    public string userAgent { get; private set; } = DefaultUserAgent;
    public int jobTimeout { get; init; } = 180;
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string libraryConnectorsFilePath => Path.Join(workingDirectory, "libraryConnectors.json");
    [JsonIgnore] public string notificationConnectorsFilePath => Path.Join(workingDirectory, "notificationConnectors.json");
    [JsonIgnore] public string jobsFolderPath => Path.Join(workingDirectory, "jobs");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    [JsonIgnore] internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public ushort? version { get; } = 2;
    public bool aprilFoolsMode { get; private set; } = true;
    [JsonIgnore]internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 250},
        {RequestType.MangaDexFeed, 250},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 250},
        {RequestType.Default, 60}
    };

    public Dictionary<RequestType, int> requestLimits { get; set; } = DefaultRequestLimits;

    public TrangaSettings(string? downloadLocation = null, string? workingDirectory = null, int? apiPortNumber = null)
    {
        string wd = workingDirectory ?? Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
        string sfp = Path.Join(wd, "settings.json");
        
        string lockFilePath = $"{sfp}.lock";
        if (File.Exists(sfp) && !File.Exists(lockFilePath))
        {//Load from settings file
            FileStream lockFile = File.Create(lockFilePath,0, FileOptions.DeleteOnClose); //lock settingsfile
            string settingsStr = File.ReadAllText(sfp);
            settingsStr = Regex.Replace(settingsStr, @"""MangaDexAuthor"": [0-9]+,", "");//https://github.com/C9Glax/tranga/pull/161 Remove sometime in the future :3
            TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(settingsStr)!;
            this.requestLimits = settings.requestLimits;
            this.userAgent = settings.userAgent;
            this.downloadLocation = downloadLocation ?? settings.downloadLocation;
            this.workingDirectory = workingDirectory ?? settings.workingDirectory;
            this.apiPortNumber = apiPortNumber ?? settings.apiPortNumber;
            lockFile.Close();  //unlock settingsfile
        }
        else if(!File.Exists(sfp))
        {//No settings file exists
            if (downloadLocation?.Length < 1 || workingDirectory?.Length < 1)
                throw new ArgumentException("Download-location and working-directory paths can not be empty!");
            this.requestLimits = DefaultRequestLimits;
            this.userAgent = DefaultUserAgent;
            this.apiPortNumber = apiPortNumber ?? 6531;
            this.downloadLocation = downloadLocation ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
            this.workingDirectory = workingDirectory ?? Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
            ExportSettings();
        }
        else
        {//Settingsfile is locked
            this.requestLimits = DefaultRequestLimits;
            this.userAgent = DefaultUserAgent;
            this.apiPortNumber = apiPortNumber!.Value;
            this.downloadLocation = downloadLocation!;
            this.workingDirectory = workingDirectory!;
        }
        UpdateDownloadLocation(this.downloadLocation, false);
    }

    public HashSet<LibraryConnector> LoadLibraryConnectors(GlobalBase clone)
    {
        if (!File.Exists(libraryConnectorsFilePath))
            return new HashSet<LibraryConnector>();
        return JsonConvert.DeserializeObject<HashSet<LibraryConnector>>(File.ReadAllText(libraryConnectorsFilePath),
            new JsonSerializerSettings()
            {
                Converters =
                {
                    new LibraryManagerJsonConverter(clone)
                }
            })!;
    }

    public HashSet<NotificationConnector> LoadNotificationConnectors(GlobalBase clone)
    {
        if (!File.Exists(notificationConnectorsFilePath))
            return new HashSet<NotificationConnector>();
        return JsonConvert.DeserializeObject<HashSet<NotificationConnector>>(File.ReadAllText(notificationConnectorsFilePath),
            new JsonSerializerSettings()
            {
                Converters =
                {
                    new NotificationManagerJsonConverter(clone)
                }
            })!;
    }

    public void UpdateAprilFoolsMode(bool enabled)
    {
        this.aprilFoolsMode = enabled;
        ExportSettings();
    }

    public void UpdateDownloadLocation(string newPath, bool moveFiles = true)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        
        if (moveFiles && Directory.Exists(this.downloadLocation))
            Directory.Move(this.downloadLocation, newPath);

        this.downloadLocation = newPath;
        ExportSettings();
    }

    public void UpdateWorkingDirectory(string newPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        Directory.Move(this.workingDirectory, newPath);
        this.workingDirectory = newPath;
        ExportSettings();
    }

    public void UpdateUserAgent(string? customUserAgent)
    {
        this.userAgent = customUserAgent ?? DefaultUserAgent;
        ExportSettings();
    }

    public void ExportSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            while(GlobalBase.IsFileInUse(settingsFilePath, null))
                Thread.Sleep(100);
        }
        else
            Directory.CreateDirectory(new FileInfo(settingsFilePath).DirectoryName!);
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public string GetFullCoverPath(Manga manga)
    {
        return Path.Join(this.coverImageCache, manga.coverFileNameInCache);
    }

    public override string ToString()
    {
        return $"TrangaSettings:\n" +
               $"\tDownloadLocation: {downloadLocation}\n" +
               $"\tworkingDirectory: {workingDirectory}\n" +
               $"\tjobsFolderPath: {jobsFolderPath}\n" +
               $"\tsettingsFilePath: {settingsFilePath}\n" +
               $"\t\tnotificationConnectors: {notificationConnectorsFilePath}\n" +
               $"\t\tlibraryConnectors: {libraryConnectorsFilePath}\n";
    }
}