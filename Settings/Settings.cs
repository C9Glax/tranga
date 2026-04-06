namespace Settings;

public static class Settings
{
    public static bool AllowNSFW { get => _allowNsfw; set => UpdateValue(ref _allowNsfw, value); }
    private static bool _allowNsfw = bool.Parse(Environment.GetEnvironmentVariable("AllowNSFW") ?? "false");

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