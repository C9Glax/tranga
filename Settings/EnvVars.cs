// ReSharper disable InconsistentNaming
namespace Settings;

public struct EnvVars
{
    public static readonly string DBName = Environment.GetEnvironmentVariable("DBName") ?? "tranga";
    public static readonly string? DBHost = Environment.GetEnvironmentVariable("DBHost");
    public static readonly string? DBUser = Environment.GetEnvironmentVariable("DBUser");
    public static readonly string? DBPass = Environment.GetEnvironmentVariable("DBPass");
    public static readonly string POSTGRES_HOST = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "tranga-pg";
    public static readonly string POSTGRES_USER = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
    public static readonly string POSTGRES_PASSWORD = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
    public static readonly int DBConnectionLifetime = int.Parse(Environment.GetEnvironmentVariable("DBConnectionLifetime") ?? "60");
    public static readonly int DBConnectionTimeout = int.Parse(Environment.GetEnvironmentVariable("DBConnectionTimeout") ?? "30");
    public static readonly int DBCommandTimeout = int.Parse(Environment.GetEnvironmentVariable("DBCommandTimeout") ?? "60");
    public static readonly string SettingsFile = Environment.GetEnvironmentVariable("SETTINGS_FILE") ?? "settings.json";
}