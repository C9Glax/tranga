using System.Reflection;

namespace Settings;

public struct Constants
{
    public static string CoverDirectory = "Covers";
    public static TimeSpan WorkerPickupWorkTimeout = TimeSpan.FromSeconds(1);
    public static TimeSpan SchedulerCreateWorkTimeout = TimeSpan.FromMinutes(1);
    public static readonly bool OpenApiDocumentationRun = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}