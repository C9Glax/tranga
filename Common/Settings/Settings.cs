using Common.Datatypes;

namespace Common.Settings;

public static class Settings
{
    public static bool AllowNSFW { get => _allowNsfw; set => UpdateValue(ref _allowNsfw, value); }
    private static bool _allowNsfw = bool.Parse(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AllowNSFW")) ? "false" : Environment.GetEnvironmentVariable("AllowNSFW")!);

    public static Language DownloadLanguage { get => _downloadLanguage; set => UpdateValue(ref _downloadLanguage, value); }
    private static Language _downloadLanguage = new (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DownloadLanguage")) ? "en" : Environment.GetEnvironmentVariable("DownloadLanguage")!);
    
    public static string ChapterNamingScheme { get => _chapterNamingScheme; set => UpdateValue(ref _chapterNamingScheme, value); }
    private static string _chapterNamingScheme = Environment.GetEnvironmentVariable("ChapterNamingScheme") ?? "?V(Vol. %V ) Ch. %C?T( - %T)";

    // ReSharper disable once RedundantAssignment
    private static void UpdateValue<T>(ref T val, T newValue)
    {
        val = newValue;
    }

    private static void ExportSettings()
    {
        // TODO
    }
    
}