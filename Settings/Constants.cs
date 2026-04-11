namespace Settings;

public struct Constants
{
    public static string CoverDirectory = "Covers";
    public static TimeSpan WorkerPickupWorkTimeout = TimeSpan.FromSeconds(1);
    public static TimeSpan SchedulerCreateWorkTimeout = TimeSpan.FromMinutes(1);
}