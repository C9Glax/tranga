using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using Newtonsoft.Json;

namespace API;

public struct TrangaSettings()
{

    [JsonIgnore]
    public static string workingDirectory => Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    [JsonIgnore]
    public static string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore]
    public static string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public string DownloadLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Manga");
    [JsonIgnore]
    internal static readonly string DefaultUserAgent = $"Tranga/2.0 ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")})";
    public string UserAgent { get; private set; } = DefaultUserAgent;
    public int ImageCompression{ get; private set; } = 40;
    public bool BlackWhiteImages { get; private set; } = false;
    public string FlareSolverrUrl { get; private set; } = string.Empty;
    /// <summary>
    /// Placeholders:
    /// %M Obj Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Obj Internal ID
    /// %Y Year (Obj)
    ///
    /// ?_(...) replace _ with a value from above:
    /// Everything inside the braces will only be added if the value of %_ is not null
    /// </summary>
    public string ChapterNamingScheme { get; private set; } = "%M - ?V(Vol.%V )Ch.%C?T( - %T)";
    public int WorkCycleTimeoutMs { get; private set; } = 20000;
    [JsonIgnore]
    internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 60},
        {RequestType.MangaDexFeed, 60},
        {RequestType.MangaDexImage, 60},
        {RequestType.MangaImage, 240},
        {RequestType.MangaCover, 60},
        {RequestType.Default, 60}
    };
    public Dictionary<RequestType, int> RequestLimits { get; private set; } = DefaultRequestLimits;

    public string DownloadLanguage { get; private set; } = "en";

    public static TrangaSettings Load()
    {
        return JsonConvert.DeserializeObject<TrangaSettings>(File.ReadAllText(settingsFilePath));
    }

    public void Save()
    {
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(this));
    }

    public void SetUserAgent(string value)
    {
        this.UserAgent = value;
        Save();
    }

    public void SetRequestLimit(RequestType type, int value)
    {
        this.RequestLimits[type] = value;
        Save();
    }

    public void ResetRequestLimits()
    {
        this.RequestLimits = DefaultRequestLimits;
        Save();
    }

    public void UpdateImageCompression(int value)
    {
        this.ImageCompression = value;
        Save();
    }

    public void SetBlackWhiteImageEnabled(bool enabled)
    {
        this.BlackWhiteImages = enabled;
        Save();
    }

    public void SetChapterNamingScheme(string scheme)
    {
        this.ChapterNamingScheme = scheme;
        Save();
    }

    public void SetFlareSolverrUrl(string url)
    {
        this.FlareSolverrUrl = url;
        Save();
    }

    public void SetDownloadLanguage(string language)
    {
        this.DownloadLanguage = language;
        Save();
    }
}