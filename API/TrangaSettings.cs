using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using API.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API;

public static class TrangaSettings
{
    public static string downloadLocation { get; private set; } = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
    public static string workingDirectory { get; private set; } = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    [JsonIgnore]
    internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public static string userAgent { get; private set; } = DefaultUserAgent;
    public static int compression{ get; private set; } = 40;
    public static bool bwImages { get; private set; } = false;
    /// <summary>
    /// Placeholders:
    /// %M Manga Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Manga Internal ID
    /// %Y Year (Manga)
    ///
    /// ?_(...) replace _ with a value from above:
    /// Everything inside the braces will only be added if the value of %_ is not null
    /// </summary>
    public static string chapterNamingScheme { get; private set; } = "%M - ?V(Vol.%V )Ch.%C?T( - %T)";
    [JsonIgnore]
    public static string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore]
    public static string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public static bool aprilFoolsMode { get; private set; } = true;
    public static int startNewJobTimeoutMs { get; private set; } = 5000;
    [JsonIgnore]
    internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 60},
        {RequestType.MangaDexFeed, 60},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 60},
        {RequestType.Default, 60}
    };
    public static Dictionary<RequestType, int> requestLimits { get; private set; } = DefaultRequestLimits;

    public static TimeSpan NotificationUrgencyDelay(NotificationUrgency urgency) => urgency switch
    {
        NotificationUrgency.High => TimeSpan.Zero,
        NotificationUrgency.Normal => TimeSpan.FromMinutes(5),
        NotificationUrgency.Low => TimeSpan.FromMinutes(10),
        _ => TimeSpan.FromHours(1)
    }; //TODO make this a setting?

    public static void Load()
    {
        if(File.Exists(settingsFilePath))
            Deserialize(File.ReadAllText(settingsFilePath));
        else return;

        Directory.CreateDirectory(downloadLocation);
        ExportSettings();
    }

    public static void UpdateAprilFoolsMode(bool enabled)
    {
        aprilFoolsMode = enabled;
        ExportSettings();
    }

    public static void UpdateCompressImages(int value)
    {
        compression = int.Clamp(value, 1, 100);
        ExportSettings();
    }

    public static void UpdateBwImages(bool enabled)
    {
        bwImages = enabled;
        ExportSettings();
    }

    public static void UpdateUserAgent(string? customUserAgent)
    {
        userAgent = customUserAgent ?? DefaultUserAgent;
        ExportSettings();
    }

    public static void UpdateRequestLimit(RequestType requestType, int newLimit)
    {
        requestLimits[requestType] = newLimit;
        ExportSettings();
    }

    public static void UpdateChapterNamingScheme(string namingScheme)
    {
        chapterNamingScheme = namingScheme;
        ExportSettings();
    }

    public static void ResetRequestLimits()
    {
        requestLimits = DefaultRequestLimits;
        ExportSettings();
    }

    public static void ExportSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            while(IsFileInUse(settingsFilePath))
                Thread.Sleep(100);
        }
        else
            Directory.CreateDirectory(new FileInfo(settingsFilePath).DirectoryName!);
        File.WriteAllText(settingsFilePath, Serialize());
    }
    
    internal static bool IsFileInUse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;
        try
        {
            using FileStream stream = new (filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public static JObject AsJObject()
    {
        JObject jobj = new ();
        jobj.Add("downloadLocation", JToken.FromObject(downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(workingDirectory));
        jobj.Add("userAgent", JToken.FromObject(userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(aprilFoolsMode));
        jobj.Add("requestLimits", JToken.FromObject(requestLimits));
        jobj.Add("compression", JToken.FromObject(compression));
        jobj.Add("bwImages", JToken.FromObject(bwImages));
        jobj.Add("startNewJobTimeoutMs", JToken.FromObject(startNewJobTimeoutMs));
        jobj.Add("chapterNamingScheme", JToken.FromObject(chapterNamingScheme));
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
        if (jobj.TryGetValue("userAgent", out JToken? ua))
            userAgent = ua.Value<string>()!;
        if (jobj.TryGetValue("aprilFoolsMode", out JToken? afm))
            aprilFoolsMode = afm.Value<bool>()!;
        if (jobj.TryGetValue("requestLimits", out JToken? rl))
            requestLimits = rl.ToObject<Dictionary<RequestType, int>>()!;
        if (jobj.TryGetValue("compression", out JToken? ci))
            compression = ci.Value<int>()!;
        if (jobj.TryGetValue("bwImages", out JToken? bwi))
            bwImages = bwi.Value<bool>()!;
        if (jobj.TryGetValue("startNewJobTimeoutMs", out JToken? snjt))
            startNewJobTimeoutMs = snjt.Value<int>()!;
        if (jobj.TryGetValue("chapterNamingScheme", out JToken? cns))
            chapterNamingScheme = cns.Value<string>()!;
    }
}