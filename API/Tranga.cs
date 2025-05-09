using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using API.Schema.NotificationConnectors;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;

namespace API;

public static class Tranga
{
    public static Thread NotificationSenderThread { get; } = new (NotificationSender);
    public static Thread JobStarterThread { get; } = new (JobStarter);
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
        Log.Info("Logger Configured.");
    }

    private static void NotificationSender(object? serviceProviderObj)
    {
        if (serviceProviderObj is null)
        {
            Log.Error("serviceProviderObj is null");
            return;
        }
        IServiceProvider serviceProvider = (IServiceProvider)serviceProviderObj!;
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext? context = scope.ServiceProvider.GetService<PgsqlContext>();
        if (context is null)
        {
            Log.Error("PgsqlContext is null");
            return;
        }

        try
        {
            //Removing Notifications from previous runs
            IQueryable<Notification> staleNotifications =
                context.Notifications.Where(n => n.Urgency < NotificationUrgency.Normal);
            context.Notifications.RemoveRange(staleNotifications);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error("Error removing stale notifications.", e);
        }
        
        while (true)
        {
            SendNotifications(serviceProvider, NotificationUrgency.High);
            SendNotifications(serviceProvider, NotificationUrgency.Normal);
            SendNotifications(serviceProvider, NotificationUrgency.Low);
            
            Thread.Sleep(2000);
        }
    }

    private static void SendNotifications(IServiceProvider serviceProvider, NotificationUrgency urgency)
    {
        Log.Info($"Sending notifications for {urgency}");
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext? context = scope.ServiceProvider.GetService<PgsqlContext>();
        if (context is null)
        {
            Log.Error("PgsqlContext is null");
            return;
        }
        
        List<Notification> notifications = context.Notifications.Where(n => n.Urgency == urgency).ToList();
        if (!notifications.Any())
            return;

        try
        {
            foreach (NotificationConnector notificationConnector in context.NotificationConnectors)
            {
                foreach (Notification notification in notifications)
                    notificationConnector.SendNotification(notification.Title, notification.Message);
            }

            context.Notifications.RemoveRange(notifications);
            context.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            Log.Error("Error sending notifications.", e);
        }
    }

    private const string TRANGA = 
        "\n\n" +
        " _______                                   \n" +
        "|_     _|.----..---.-..-----..-----..---.-.\n" +
        "  |   |  |   _||  _  ||     ||  _  ||  _  |\n" +
        "  |___|  |__|  |___._||__|__||___  ||___._|\n" +
        "                             |_____|       \n\n";
    private static readonly Dictionary<Thread, Job> RunningJobs = new();
    private static void JobStarter(object? serviceProviderObj)
    {
        if (serviceProviderObj is null)
        {
            Log.Error("serviceProviderObj is null");
            return;
        }
        IServiceProvider serviceProvider = (IServiceProvider)serviceProviderObj;
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext? context = scope.ServiceProvider.GetService<PgsqlContext>();
        if (context is null)
        {
            Log.Error("PgsqlContext is null");
            return;
        }

        Log.Info(TRANGA);
        Log.Info("JobStarter Thread running.");
        while (true)
        {
            //Update finished Jobs to new states
            List<Job> completedJobs = context.Jobs.Where(j => j.state == JobState.Completed).ToList();
            foreach (Job completedJob in completedJobs)
                if (completedJob.RecurrenceMs <= 0)
                    context.Jobs.Remove(completedJob);
                else
                {
                    completedJob.state = JobState.CompletedWaiting;
                    completedJob.LastExecution = DateTime.UtcNow;
                }
            List<Job> failedJobs = context.Jobs.Where(j => j.state == JobState.Failed).ToList();
            foreach (Job failedJob in failedJobs)
            {
                failedJob.Enabled = false;
                failedJob.LastExecution = DateTime.UtcNow;
            }

            //Retrieve waiting and due Jobs
            List<Job> waitingJobs = context.Jobs.Where(j =>
                j.Enabled && (j.state == JobState.FirstExecution || j.state == JobState.CompletedWaiting)).ToList();
            List<Job> runningJobs = context.Jobs.Where(j => j.state == JobState.Running).ToList();
            List<Job> dueJobs = waitingJobs.Where(j => j.NextExecution < DateTime.UtcNow).ToList();

            List<MangaConnector> busyConnectors = GetBusyConnectors(runningJobs);
            List<Job> startJobs = FilterJobPreconditions(dueJobs, busyConnectors);
            
            //Start Jobs that are allowed to run (preconditions match)
            foreach (Job job in startJobs)
            {
                Thread t = new(() =>
                {
                    job.Run(serviceProvider);
                });
                RunningJobs.Add(t, job);
                t.Start();
            }
            Log.Debug($"Jobs Completed: {completedJobs.Count} Failed: {failedJobs.Count} Running: {runningJobs.Count}\n" +
                      $"Waiting: {waitingJobs.Count}\n" +
                      $"\tof which Due: {dueJobs.Count}\n" +
                      $"\t\tof which Started: {startJobs.Count}");

            (Thread, Job)[] removeFromThreadsList = RunningJobs.Where(t => !t.Key.IsAlive)
                .Select(t => (t.Key, t.Value)).ToArray();
            Log.Debug($"Remove from Threads List: {removeFromThreadsList.Length}");
            foreach ((Thread thread, Job job) thread in removeFromThreadsList)
            {
                RunningJobs.Remove(thread.thread);
            }

            try
            {
                context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error("Failed saving Job changes.", e);
            }
            Thread.Sleep(TrangaSettings.startNewJobTimeoutMs);
        }
    }

    private static List<MangaConnector> GetBusyConnectors(List<Job> runningJobs)
    {
        HashSet<MangaConnector> busyConnectors = new();
        foreach (Job runningJob in runningJobs)
        {
            if(GetJobConnector(runningJob) is { } mangaConnector)
                busyConnectors.Add(mangaConnector);
        }
        return busyConnectors.ToList();
    }

    private static List<Job> FilterJobPreconditions(List<Job> dueJobs, List<MangaConnector> busyConnectors) =>
        dueJobs
            .Where(j => j.DependenciesFulfilled)
            .Where(j =>
            {
                //Filter jobs with busy connectors
                if (GetJobConnector(j) is { } mangaConnector)
                    return busyConnectors.Contains(mangaConnector) == false;
                return true;
            })
            .ToList();

    private static MangaConnector? GetJobConnector(Job job)
    {
        if (job is DownloadAvailableChaptersJob dacj)
            return dacj.Manga.MangaConnector;
        if (job is DownloadMangaCoverJob dmcj)
            return  dmcj.Manga.MangaConnector;
        if (job is DownloadSingleChapterJob dscj)
            return  dscj.Chapter.ParentManga.MangaConnector;
        if (job is RetrieveChaptersJob rcj)
            return rcj.Manga.MangaConnector;
        return null;
    }
}