using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.IO.UnixFileMode;

namespace API;

public static class TrangaSettings
{
    public static string downloadLocation { get; private set; } = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
    public static string workingDirectory { get; private set; } = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    public static int apiPortNumber { get; private set; } = 6531;
    [JsonIgnore]
    internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public static string userAgent { get; private set; } = DefaultUserAgent;
    public static int compression{ get; private set; } = 40;
    public static bool bwImages { get; private set; } = false;
    [JsonIgnore]
    public static string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore]
    public static string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public static bool aprilFoolsMode { get; private set; } = true;
    [JsonIgnore]
    internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 250},
        {RequestType.MangaDexFeed, 250},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 250},
        {RequestType.Default, 60}
    };
    public static Dictionary<RequestType, int> requestLimits { get; set; } = DefaultRequestLimits;

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

    public static void UpdateDownloadLocation(string newPath, bool moveFiles = true)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath, GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        
        if (moveFiles)
            MoveContentsOfDirectoryTo(TrangaSettings.downloadLocation, newPath);
        
        TrangaSettings.downloadLocation = newPath;
        ExportSettings();
    }

    private static void MoveContentsOfDirectoryTo(string oldDir, string newDir)
    {
        string[] directoryPaths = Directory.GetDirectories(oldDir);
        string[] filePaths = Directory.GetFiles(oldDir);
        foreach (string file in filePaths)
        {
            string newPath = Path.Join(newDir, Path.GetFileName(file));
            File.Move(file, newPath, true);
        }
        foreach(string directory in directoryPaths)
        {
            string? dirName = Path.GetDirectoryName(directory);
            if(dirName is null)
                continue;
            string newPath = Path.Join(newDir, dirName);
            if(Directory.Exists(newPath))
                MoveContentsOfDirectoryTo(directory, newPath);
            else
                Directory.Move(directory, newPath);
        }
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
        JObject jobj = new JObject();
        jobj.Add("downloadLocation", JToken.FromObject(downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(workingDirectory));
        jobj.Add("apiPortNumber", JToken.FromObject(apiPortNumber));
        jobj.Add("userAgent", JToken.FromObject(userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(aprilFoolsMode));
        jobj.Add("requestLimits", JToken.FromObject(requestLimits));
        jobj.Add("compression", JToken.FromObject(compression));
        jobj.Add("bwImages", JToken.FromObject(bwImages));
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
        if (jobj.TryGetValue("compression", out JToken? ci))
            compression = ci.Value<int>()!;
        if (jobj.TryGetValue("bwImages", out JToken? bwi))
            bwImages = bwi.Value<bool>()!;
    }
}