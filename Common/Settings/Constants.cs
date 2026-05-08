using System.Reflection;

namespace Common.Settings;

public struct Constants
{
    public const string MangaDirectory = "Mangas";
    public const string CoverDirectory = "Covers";
    public static readonly TimeSpan WorkerPickupWorkTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan SchedulerCreateWorkTimeout = TimeSpan.FromSeconds(5);
    public static readonly bool OpenApiDocumentationRun = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}