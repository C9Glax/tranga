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
    public static readonly int POSTGRES_PORT = Environment.GetEnvironmentVariable("POSTGRES_PORT") is { } var ? int.Parse(var) : 5432;
    public static readonly int DBConnectionLifetime = Environment.GetEnvironmentVariable("DBConnectionLifetime") is { } var ? int.Parse(var) : 60;
    public static readonly int DBConnectionTimeout = Environment.GetEnvironmentVariable("DBConnectionTimeout") is { } var ? int.Parse(var) : 30;
    public static readonly int DBCommandTimeout = Environment.GetEnvironmentVariable("DBCommandTimeout") is { } var ? int.Parse(var) : 60;
    public static readonly string SettingsFile = Environment.GetEnvironmentVariable("SETTINGS_FILE") ?? "settings.json";
    public static readonly int WorkersCount = Environment.GetEnvironmentVariable("WORKERS_COUNT") is { } var ? int.Parse(var) : Environment.ProcessorCount / 2;
}