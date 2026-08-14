// ReSharper disable InconsistentNaming
namespace Tranga.AppHost;

public struct EnvVars
{
    public static readonly string DBName = Environment.GetEnvironmentVariable("DBName") ?? "tranga";
    public static readonly string POSTGRES_HOST = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "tranga-pg";
    public static readonly bool ENABLE_SUWAYOMI = Environment.GetEnvironmentVariable("ENABLE_SUWAYOMI") is { } enableSuwayomi && bool.TryParse(enableSuwayomi, out bool suwayomiEnabled) && suwayomiEnabled;
    public static readonly string SUWAYOMI_URL = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUWAYOMI_URL")) ? "http://suwayomi:4567" : Environment.GetEnvironmentVariable("SUWAYOMI_URL")!;
}