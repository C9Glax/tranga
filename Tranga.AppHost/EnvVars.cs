// ReSharper disable InconsistentNaming
namespace Tranga.AppHost;

public struct EnvVars
{
    public static readonly string DBName = Environment.GetEnvironmentVariable("DBName") ?? "tranga";
    public static readonly string POSTGRES_HOST = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "tranga-pg";
}