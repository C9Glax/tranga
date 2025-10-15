using SixLabors.ImageSharp;

namespace API;

public struct Constants
{
    public static readonly Size ImageSmSize = new (225, 320);
    public static readonly Size ImageMdSize = new (450, 640);
    public static readonly Size ImageLgSize = new (900, 1280);
    
    public static readonly string PostgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "tranga-pg:5432";
    public static readonly string PostgresDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
    public static readonly string PostgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
    public static readonly string PostgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
    public static readonly int PostgresConnectionTimeout = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_TIMEOUT") ?? "30");
    public static readonly int PostgresCommandTimeout =  int.Parse(Environment.GetEnvironmentVariable("POSTGRES_COMMAND_TIMEOUT") ?? "60");

    public static readonly bool UpdateChaptersDownloadedBeforeStarting = bool.Parse(Environment.GetEnvironmentVariable("CHECK_CHAPTERS_BEFORE_START") ?? "true");
    
    public static readonly bool CreateComicInfoXml = bool.Parse(Environment.GetEnvironmentVariable("CREATE_COMICINFO_XML") ?? "true");
}