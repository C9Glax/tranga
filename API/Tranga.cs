using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using API.Schema.NotificationConnectors;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API;

public static class Tranga
{

    // ReSharper disable once InconsistentNaming
    private const string TRANGA = 
        "\n\n" +
        " _______                                 v2\n" +
        "|_     _|.----..---.-..-----..-----..---.-.\n" +
        "  |   |  |   _||  _  ||     ||  _  ||  _  |\n" +
        "  |___|  |__|  |___._||__|__||___  ||___._|\n" +
        "                             |_____|       \n\n";
    
    public static Thread NotificationSenderThread { get; } = new (NotificationSender);
    public static Thread JobStarterThread { get; } = new (JobStarter);
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
        Log.Info("Logger Configured.");
        Log.Info(TRANGA);
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
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();

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
        Log.Debug($"Sending notifications for {urgency}");
        using IServiceScope scope = serviceProvider.CreateScope();
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        
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
    private static readonly Dictionary<Thread, Job> RunningJobs = new();
    private static void JobStarter(object? serviceProviderObj)
    {
        Log.Info("JobStarter Thread running.");
        if (serviceProviderObj is null)
        {
            Log.Error("serviceProviderObj is null");
            return;
        }
        IServiceProvider serviceProvider = (IServiceProvider)serviceProviderObj;
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext context = scope.ServiceProvider.GetRequiredService<PgsqlContext>();

        DateTime lastContextUpdate = DateTime.UnixEpoch;
        
        while (true)
        {
            if (lastContextUpdate.AddMilliseconds(TrangaSettings.startNewJobTimeoutMs * 10) < DateTime.UtcNow)
            {
                Log.Info("Loading Jobs...");
                context.Jobs.Load();
                lastContextUpdate = DateTime.UtcNow;
                Log.Info("Jobs Loaded!");
            }
            foreach (EntityEntry entityEntry in context.ChangeTracker.Entries().ToArray())
                entityEntry.Reload();
            //Update finished Jobs to new states
            List<Job> completedJobs = context.Jobs.Local.Where(j => j.state == JobState.Completed).ToList();
            foreach (Job completedJob in completedJobs)
                if (completedJob.RecurrenceMs <= 0)
                    context.Jobs.Remove(completedJob);
                else
                {
                    completedJob.state = JobState.CompletedWaiting;
                    completedJob.LastExecution = DateTime.UtcNow;
                }
            List<Job> failedJobs = context.Jobs.Local.Where(j => j.state == JobState.Failed).ToList();
            foreach (Job failedJob in failedJobs)
            {
                failedJob.Enabled = false;
                failedJob.LastExecution = DateTime.UtcNow;
            }

            //Retrieve waiting and due Jobs
            List<Job> runningJobs = context.Jobs.Local.Where(j => j.state == JobState.Running).ToList();
            
            List<MangaConnector> busyConnectors = GetBusyConnectors(runningJobs);

            List<Job> waitingJobs = GetWaitingJobs(context.Jobs.Local.ToList());
            List<Job> dueJobs = FilterDueJobs(waitingJobs);
            List<Job> jobsWithoutBusyConnectors = FilterJobWithBusyConnectors(dueJobs, busyConnectors);
            List<Job> jobsWithoutMissingDependencies = FilterJobDependencies(context, jobsWithoutBusyConnectors);

            List<Job> jobsWithoutDownloading =
                jobsWithoutMissingDependencies
                    .Where(j => j.JobType != JobType.DownloadSingleChapterJob)
                    .ToList();
            List<Job> firstChapterPerConnector =
                jobsWithoutMissingDependencies
                    .Where(j => j.JobType == JobType.DownloadSingleChapterJob)
                    .OrderBy(j =>
                    {
                        DownloadSingleChapterJob dscj = (DownloadSingleChapterJob)j;
                        return dscj.Chapter;
                    })
                    .DistinctBy(j =>
                    {
                        DownloadSingleChapterJob dscj = (DownloadSingleChapterJob)j;
                        return dscj.Chapter.ParentManga.MangaConnector;
                    })
                    .ToList();

            List<Job> startJobs = jobsWithoutDownloading.Concat(firstChapterPerConnector).ToList();
            
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
                      $"\t\tof which Started: {jobsWithoutMissingDependencies.Count}");

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
    
    private static List<Job> GetWaitingJobs(List<Job> jobs) =>
        jobs
            .Where(j =>
                j.Enabled &&
                (j.state == JobState.FirstExecution || j.state == JobState.CompletedWaiting))
            .ToList();

    private static List<Job> FilterDueJobs(List<Job> jobs) =>
        jobs
            .Where(j => j.NextExecution < DateTime.UtcNow)
            .ToList();

    private static List<Job> FilterJobDependencies(PgsqlContext context, List<Job> jobs) =>
        jobs
            .Where(j =>
            {
                Log.Debug($"Loading Job Preconditions {j}...");
                context.Entry(j).Collection(j => j.DependsOnJobs).Load();
                Log.Debug($"Loaded Job Preconditions {j}!");
                return j.DependenciesFulfilled;
            })
            .ToList();

    private static List<Job> FilterJobWithBusyConnectors(List<Job> jobs, List<MangaConnector> busyConnectors) =>
        jobs
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