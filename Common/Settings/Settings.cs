using Common.Datatypes;

namespace Common.Settings;

public static class Settings
{
    public static bool AllowNSFW { get => _allowNsfw; set => UpdateValue(ref _allowNsfw, value); }
    private static bool _allowNsfw = bool.Parse(Environment.GetEnvironmentVariable("AllowNSFW") ?? "false");
    
    public static Language DownloadLanguage { get => _downloadLanguage; set => UpdateValue(ref _downloadLanguage, value); }
    private static Language _downloadLanguage = new (Environment.GetEnvironmentVariable("DownloadLanguage") ?? "en");

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