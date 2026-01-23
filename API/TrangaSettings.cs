using System.Runtime.InteropServices;
using API.Workers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace API;

public struct TrangaSettings
{
    [JsonIgnore] public static int Port => int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "6531");
    [JsonIgnore] public static bool Debug => bool.Parse(Environment.GetEnvironmentVariable("DEBUG") ?? "false");
    [JsonIgnore] public static string AppData => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Debug ? "./debug" :"/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    [JsonIgnore] public static string WorkingDirectory => Path.Join(AppData, "tranga-api");
    [JsonIgnore] public static string SettingsFilePath => Path.Join(WorkingDirectory, "settings.json");
    [JsonIgnore] public static string CoverImageCache => Path.Join(WorkingDirectory, "imageCache");
    [JsonIgnore] public static string CoverImageCacheOriginal => Path.Join(CoverImageCache, "original");
    [JsonIgnore] public static string CoverImageCacheLarge => Path.Join(CoverImageCache, "large");
    [JsonIgnore] public static string CoverImageCacheMedium => Path.Join(CoverImageCache, "medium");
    [JsonIgnore] public static string CoverImageCacheSmall => Path.Join(CoverImageCache, "small");
    public static string DefaultDownloadLocation => Environment.GetEnvironmentVariable("DOWNLOAD_LOCATION") ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Manga"));
    [JsonIgnore] internal static readonly string DefaultUserAgent = $"Tranga/2.0 ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")})";
    public string UserAgent { get; set; } = DefaultUserAgent;
    public int ImageCompression{ get; set; } = 40;
    public bool BlackWhiteImages { get; set; } = false;
    public string FlareSolverrUrl { get; set; } = Environment.GetEnvironmentVariable("FLARESOLVERR_URL") ?? string.Empty;
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
    public string ChapterNamingScheme { get; set; } = "%M - ?V(Vol.%V )Ch.%C?T( - %T)";
    public int WorkCycleTimeoutMs { get; set; } = 20000;

    public string DownloadLanguage { get; set; } = "en";
    
    public int MaxConcurrentDownloads { get; set; } = (int)Math.Max(Environment.ProcessorCount * 0.75, 1); // Minimum of 1 Tasks, maximum of 0.75 per Core

    public int MaxConcurrentWorkers { get; set; } = Math.Max(Environment.ProcessorCount, 4); // Minimum of 4 Tasks, maximum of 1 per Core

    public LibraryRefreshSetting LibraryRefreshSetting { get; set; } = LibraryRefreshSetting.AfterMangaFinished;

    public int RefreshLibraryWhileDownloadingEveryMinutes { get; set; } = 10;

    public TrangaSettings()
    {
        Directory.CreateDirectory(WorkingDirectory);
    }

    public static TrangaSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
            new TrangaSettings().Save();
        return JsonConvert.DeserializeObject<TrangaSettings>(File.ReadAllText(SettingsFilePath), new StringEnumConverter());
    }

    public void Save()
    {
        File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented, new StringEnumConverter()));
    }

    public void SetUserAgent(string value)
    {
        this.UserAgent = value;
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

    public void SetMaxConcurrentDownloads(int value)
    {
        this.MaxConcurrentDownloads = value;
        Save();
    }

    public void SetMaxConcurrentWorkers(int value)
    {
        this.MaxConcurrentWorkers = value;
        Save();
    }

    public void SetLibraryRefreshSetting(LibraryRefreshSetting setting)
    {
        this.LibraryRefreshSetting = setting;
        Save();
    }

    public void SetRefreshLibraryWhileDownloadingEveryMinutes(int value)
    {
        this.RefreshLibraryWhileDownloadingEveryMinutes = value;
        Save();
    }
}