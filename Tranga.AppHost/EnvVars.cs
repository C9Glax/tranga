// ReSharper disable InconsistentNaming
namespace Tranga.AppHost;

public struct EnvVars
{
    public static readonly string DBName = Environment.GetEnvironmentVariable("DBName") ?? "tranga";
    public static readonly string POSTGRES_HOST = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "tranga-pg";
    public static readonly string MangaDirectory = Environment.GetEnvironmentVariable("MangaDirectory") ?? "Mangas";
    public static readonly string CoverDirectory = Environment.GetEnvironmentVariable("CoverDirectory") ?? "Covers";
}