using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.LibraryConnectors;
using Tranga.MangaConnectors;
using Tranga.NotificationConnectors;
using static System.IO.UnixFileMode;

namespace Tranga;

public static class TrangaSettings
{
    [JsonIgnore] internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public static string downloadLocation { get; private set; } = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
    public static string workingDirectory { get; private set; } = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    public static int apiPortNumber { get; private set; } = 6531;
    public static string userAgent { get; private set; } = DefaultUserAgent;
    [JsonIgnore] public static string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public static string libraryConnectorsFilePath => Path.Join(workingDirectory, "libraryConnectors.json");
    [JsonIgnore] public static string notificationConnectorsFilePath => Path.Join(workingDirectory, "notificationConnectors.json");
    [JsonIgnore] public static string jobsFolderPath => Path.Join(workingDirectory, "jobs");
    [JsonIgnore] public static string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public static ushort? version { get; } = 2;
    public static bool aprilFoolsMode { get; private set; } = true;
    [JsonIgnore]internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 250},
        {RequestType.MangaDexFeed, 250},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 250},
        {RequestType.Default, 60}
    };

    public static Dictionary<RequestType, int> requestLimits { get; set; } = DefaultRequestLimits;

    public static void LoadFromWorkingDirectory(string directory)
    {
        TrangaSettings.workingDirectory = directory;
        if (!File.Exists(settingsFilePath))
        {
            return;
        }
        else
        {
            Deserialize(File.ReadAllText(settingsFilePath));
        }

        Directory.CreateDirectory(downloadLocation);
        Directory.CreateDirectory(workingDirectory);
        ExportSettings();
    }

    public static void CreateOrUpdate(string? downloadDirectory = null, string? pWorkingDirectory = null, int? pApiPortNumber = null, string? pUserAgent = null, bool? pAprilFoolsMode = null)
    {
        TrangaSettings.downloadLocation = downloadDirectory ?? TrangaSettings.downloadLocation;
        TrangaSettings.workingDirectory = pWorkingDirectory ?? TrangaSettings.workingDirectory;
        TrangaSettings.apiPortNumber = pApiPortNumber ?? TrangaSettings.apiPortNumber;
        TrangaSettings.userAgent = pUserAgent ?? TrangaSettings.userAgent;
        TrangaSettings.aprilFoolsMode = pAprilFoolsMode ?? TrangaSettings.aprilFoolsMode;
        Directory.CreateDirectory(downloadLocation);
        Directory.CreateDirectory(workingDirectory);
        ExportSettings();
    }

    public static HashSet<LibraryConnector> LoadLibraryConnectors(GlobalBase clone)
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

    public static HashSet<NotificationConnector> LoadNotificationConnectors(GlobalBase clone)
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

    public static void UpdateAprilFoolsMode(bool enabled)
    {
        TrangaSettings.aprilFoolsMode = enabled;
        ExportSettings();
    }

    public static void UpdateDownloadLocation(string newPath, bool moveFiles = true)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        
        if (moveFiles && Directory.Exists(TrangaSettings.downloadLocation))
            Directory.Move(TrangaSettings.downloadLocation, newPath);

        TrangaSettings.downloadLocation = newPath;
        ExportSettings();
    }

    public static void UpdateWorkingDirectory(string newPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        Directory.Move(TrangaSettings.workingDirectory, newPath);
        TrangaSettings.workingDirectory = newPath;
        ExportSettings();
    }

    public static void UpdateUserAgent(string? customUserAgent)
    {
        TrangaSettings.userAgent = customUserAgent ?? DefaultUserAgent;
        ExportSettings();
    }

    public static void UpdateRateLimit(RequestType requestType, int newLimit)
    {
        TrangaSettings.requestLimits[requestType] = newLimit;
        ExportSettings();
    }

    public static void ResetRateLimits()
    {
        TrangaSettings.requestLimits = DefaultRequestLimits;
        ExportSettings();
    }

    public static void ExportSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            while(GlobalBase.IsFileInUse(settingsFilePath, null))
                Thread.Sleep(100);
        }
        else
            Directory.CreateDirectory(new FileInfo(settingsFilePath).DirectoryName!);
        File.WriteAllText(settingsFilePath, Serialize());
    }

    public static string Serialize()
    {
        JObject jobj = new JObject();
        jobj.Add("downloadLocation", JToken.FromObject(TrangaSettings.downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(TrangaSettings.workingDirectory));
        jobj.Add("apiPortNumber", JToken.FromObject(TrangaSettings.apiPortNumber));
        jobj.Add("userAgent", JToken.FromObject(TrangaSettings.userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(TrangaSettings.aprilFoolsMode));
        jobj.Add("version", JToken.FromObject(TrangaSettings.version));
        jobj.Add("requestLimits", JToken.FromObject(TrangaSettings.requestLimits));
        return jobj.ToString();
    }

    public static void Deserialize(string serialized)
    {
        JObject jobj = JObject.Parse(serialized);
        if (jobj.TryGetValue("downloadLocation", out JToken? dl))
            TrangaSettings.downloadLocation = dl.Value<string>()!;
        if (jobj.TryGetValue("workingDirectory", out JToken? wd))
            TrangaSettings.workingDirectory = wd.Value<string>()!;
        if (jobj.TryGetValue("apiPortNumber", out JToken? apn))
            TrangaSettings.apiPortNumber = apn.Value<int>();
        if (jobj.TryGetValue("userAgent", out JToken? ua))
            TrangaSettings.userAgent = ua.Value<string>()!;
        if (jobj.TryGetValue("aprilFoolsMode", out JToken? afm))
            TrangaSettings.aprilFoolsMode = afm.Value<bool>()!;
        if (jobj.TryGetValue("requestLimits", out JToken? rl))
            TrangaSettings.requestLimits = rl.ToObject<Dictionary<RequestType, int>>()!;
    }
}