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
    public static bool bufferLibraryUpdates { get; private set; } = false;
    public static bool bufferNotifications { get; private set; } = false;
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
        if(File.Exists(settingsFilePath))
            Deserialize(File.ReadAllText(settingsFilePath));
        else return;

        Directory.CreateDirectory(downloadLocation);
        Directory.CreateDirectory(workingDirectory);
        ExportSettings();
    }

    public static void CreateOrUpdate(string? downloadDirectory = null, string? pWorkingDirectory = null, int? pApiPortNumber = null, string? pUserAgent = null, bool? pAprilFoolsMode = null, bool? pBufferLibraryUpdates = null, bool? pBufferNotifications = null)
    {
        if(pWorkingDirectory is null && File.Exists(settingsFilePath))
            LoadFromWorkingDirectory(workingDirectory);
        downloadLocation = downloadDirectory ?? downloadLocation;
        workingDirectory = pWorkingDirectory ?? workingDirectory;
        apiPortNumber = pApiPortNumber ?? apiPortNumber;
        userAgent = pUserAgent ?? userAgent;
        aprilFoolsMode = pAprilFoolsMode ?? aprilFoolsMode;
        bufferLibraryUpdates = pBufferLibraryUpdates ?? bufferLibraryUpdates;
        bufferNotifications = pBufferNotifications ?? bufferNotifications;
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
        aprilFoolsMode = enabled;
        ExportSettings();
    }

    public static void UpdateDownloadLocation(string newPath, bool moveFiles = true)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        
        if (moveFiles && Directory.Exists(downloadLocation))
            Directory.Move(downloadLocation, newPath);

        downloadLocation = newPath;
        ExportSettings();
    }

    public static void UpdateWorkingDirectory(string newPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        Directory.Move(workingDirectory, newPath);
        workingDirectory = newPath;
        ExportSettings();
    }

    public static void UpdateUserAgent(string? customUserAgent)
    {
        userAgent = customUserAgent ?? DefaultUserAgent;
        ExportSettings();
    }

    public static void UpdateRateLimit(RequestType requestType, int newLimit)
    {
        requestLimits[requestType] = newLimit;
        ExportSettings();
    }

    public static void ResetRateLimits()
    {
        requestLimits = DefaultRequestLimits;
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

    public static JObject AsJObject()
    {
        JObject jobj = new JObject();
        jobj.Add("downloadLocation", JToken.FromObject(downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(workingDirectory));
        jobj.Add("apiPortNumber", JToken.FromObject(apiPortNumber));
        jobj.Add("userAgent", JToken.FromObject(userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(aprilFoolsMode));
        jobj.Add("version", JToken.FromObject(version));
        jobj.Add("requestLimits", JToken.FromObject(requestLimits));
        jobj.Add("bufferLibraryUpdates", JToken.FromObject(bufferLibraryUpdates));
        jobj.Add("bufferNotifications", JToken.FromObject(bufferNotifications));
        return jobj;
    }

    public static string Serialize() => AsJObject().ToString();

    public static void Deserialize(string serialized)
    {
        JObject jobj = JObject.Parse(serialized);
        if (jobj.TryGetValue("downloadLocation", out JToken? dl))
            downloadLocation = dl.Value<string>()!;
        if (jobj.TryGetValue("workingDirectory", out JToken? wd))
            workingDirectory = wd.Value<string>()!;
        if (jobj.TryGetValue("apiPortNumber", out JToken? apn))
            apiPortNumber = apn.Value<int>();
        if (jobj.TryGetValue("userAgent", out JToken? ua))
            userAgent = ua.Value<string>()!;
        if (jobj.TryGetValue("aprilFoolsMode", out JToken? afm))
            aprilFoolsMode = afm.Value<bool>()!;
        if (jobj.TryGetValue("requestLimits", out JToken? rl))
            requestLimits = rl.ToObject<Dictionary<RequestType, int>>()!;
        if (jobj.TryGetValue("bufferLibraryUpdates", out JToken? blu))
            bufferLibraryUpdates = blu.Value<bool>()!;
        if (jobj.TryGetValue("bufferNotifications", out JToken? bn))
            bufferNotifications = bn.Value<bool>()!;
    }
}