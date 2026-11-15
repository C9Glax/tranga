using Common.Datatypes;

namespace Common.Settings;

public static class Settings
{
    /// <summary>
    /// The value of the <c>AllowNSFW</c> environment variable when it is set to a valid boolean, otherwise <c>null</c>.
    /// </summary>
    private static bool? _allowNsfwFromEnv = bool.TryParse(Environment.GetEnvironmentVariable("AllowNSFW"), out bool envValue) ? envValue : null;
    private static bool _allowNsfw;

    /// <summary>
    /// Whether NSFW content is allowed. When the <c>AllowNSFW</c> environment variable is set it is final
    /// (see <see cref="AllowNSFWLocked"/>); otherwise the value can be changed at runtime.
    /// </summary>
    public static bool AllowNSFW
    {
        get => _allowNsfwFromEnv ?? _allowNsfw;
        set
        {
            if (AllowNSFWLocked)
                throw new InvalidOperationException("AllowNSFW is set through the AllowNSFW environment variable and cannot be changed at runtime.");
            UpdateValue(ref _allowNsfw, value);
        }
    }

    /// <summary>Whether <see cref="AllowNSFW"/> is fixed by the <c>AllowNSFW</c> environment variable.</summary>
    public static bool AllowNSFWLocked => _allowNsfwFromEnv.HasValue;

    public static Language DownloadLanguage { get => _downloadLanguage; set => UpdateValue(ref _downloadLanguage, value); }
    private static Language _downloadLanguage = new (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DownloadLanguage")) ? "en" : Environment.GetEnvironmentVariable("DownloadLanguage")!);
    
    public static string ChapterNamingScheme { get => _chapterNamingScheme; set => UpdateValue(ref _chapterNamingScheme, value); }
    private static string _chapterNamingScheme = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ChapterNamingScheme")) ? "?V(Vol. %V ) Ch. %C?T( - %T)" : Environment.GetEnvironmentVariable("ChapterNamingScheme")!;

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