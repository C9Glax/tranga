using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using API.Schema.NotificationConnectors;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;

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

    internal static void RemoveStaleFiles(IServiceProvider serviceProvider)
    {
        Log.Info($"Removing stale files...");
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext context = scope.ServiceProvider.GetRequiredService<PgsqlContext>();
        string[] usedFiles = context.Mangas.Select(m => m.CoverFileNameInCache).Where(s => s != null).ToArray()!;
        string[] extraneousFiles = new DirectoryInfo(TrangaSettings.coverImageCache).GetFiles()
            .Where(f => usedFiles.Contains(f.FullName) == false)
            .Select(f => f.FullName)
            .ToArray();
        foreach (string path in extraneousFiles)
        {
            Log.Info($"Deleting {path}");
            File.Delete(path);
        }
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
        
        while (true)
        {            
            Log.Debug("Starting Job-Cycle...");
            DateTime cycleStart = DateTime.UtcNow;
            PgsqlContext cycleContext = scope.ServiceProvider.GetRequiredService<PgsqlContext>();
            Log.Debug("Loading Jobs...");
            DateTime loadStart = DateTime.UtcNow;
            Log.Debug($"Jobs Loaded! (took {DateTime.UtcNow.Subtract(loadStart).TotalMilliseconds}ms)");
            //Update finished Jobs to new states
            IQueryable<Job> completedJobs = cycleContext.Jobs.Where(j => j.state == JobState.Completed);
            foreach (Job completedJob in completedJobs)
                if (completedJob.RecurrenceMs <= 0)
                {
                    cycleContext.Jobs.Remove(completedJob);
                }

            //Retrieve waiting and due Jobs
            IQueryable<Job> runningJobs = cycleContext.Jobs.Where(j => j.state == JobState.Running);
            
            DateTime filterStart = DateTime.UtcNow;
            Log.Debug("Filtering Jobs...");
            List<MangaConnector> busyConnectors = GetBusyConnectors(runningJobs);

            IQueryable<Job> waitingJobs = cycleContext.Jobs.Where(j => j.state == JobState.CompletedWaiting || j.state == JobState.FirstExecution);
            List<Job> dueJobs = FilterDueJobs(waitingJobs);
            List<Job> jobsWithoutBusyConnectors = FilterJobWithBusyConnectors(dueJobs, busyConnectors);
            List<Job> jobsWithoutMissingDependencies = FilterJobDependencies(jobsWithoutBusyConnectors);

            List<Job> jobsWithoutDownloading =
                jobsWithoutMissingDependencies
                    .Where(j => j.JobType != JobType.DownloadSingleChapterJob)
                    .DistinctBy(j => j.JobType)
                    .ToList();
            List<Job> firstChapterPerConnector =
                jobsWithoutMissingDependencies
                    .Where(j => j.JobType == JobType.DownloadSingleChapterJob)
                    .AsEnumerable()
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
            Log.Debug($"Jobs Filtered! (took {DateTime.UtcNow.Subtract(filterStart).TotalMilliseconds}ms)");
            
            
            //Start Jobs that are allowed to run (preconditions match)
            foreach (Job job in startJobs)
            {
                Thread t = new(() =>
                {
                    using IServiceScope jobScope = serviceProvider.CreateScope();
                    PgsqlContext jobContext = jobScope.ServiceProvider.GetRequiredService<PgsqlContext>();
                    jobContext.Jobs.Find(job.JobId)?.Run(jobContext); //FIND the job IN THE NEW CONTEXT!!!!!!! SO WE DON'T GET TRACKING PROBLEMS AND AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                });
                RunningJobs.Add(t, job);
                t.Start();
            }
            Log.Debug($"Jobs Completed: {completedJobs.Count()} Running: {runningJobs.Count()}\n" +
                      $"Waiting: {waitingJobs.Count()}\n" +
                      $"\tof which Due: {dueJobs.Count()}\n" +
                      $"\t\tof which can be started: {jobsWithoutMissingDependencies.Count()}\n" +
                      $"\t\t\tof which started: {startJobs.Count()}");

            (Thread, Job)[] removeFromThreadsList = RunningJobs.Where(t => !t.Key.IsAlive)
                .Select(t => (t.Key, t.Value)).ToArray();
            Log.Debug($"Remove from Threads List: {removeFromThreadsList.Length}");
            foreach ((Thread thread, Job job) thread in removeFromThreadsList)
            {
                RunningJobs.Remove(thread.thread);
            }

            try
            {
                cycleContext.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error("Failed saving Job changes.", e);
            }
            Log.Debug($"Job-Cycle over! (took {DateTime.UtcNow.Subtract(cycleStart).TotalMilliseconds}ms)");
            Thread.Sleep(TrangaSettings.startNewJobTimeoutMs);
        }
    }

    private static List<MangaConnector> GetBusyConnectors(IQueryable<Job> runningJobs)
    {
        HashSet<MangaConnector> busyConnectors = new();
        foreach (Job runningJob in runningJobs)
        {
            if(GetJobConnector(runningJob) is { } mangaConnector)
                busyConnectors.Add(mangaConnector);
        }
        return busyConnectors.ToList();
    }

    private static List<Job> FilterDueJobs(IQueryable<Job> jobs) =>
        jobs.ToList()
            .Where(j => j.NextExecution < DateTime.UtcNow)
            .ToList();

    private static List<Job> FilterJobDependencies(List<Job> jobs) =>
        jobs
            .Where(job => job.DependsOnJobs.All(j => j.IsCompleted))
            .ToList();

    private static List<Job> FilterJobWithBusyConnectors(List<Job> jobs, List<MangaConnector> busyConnectors) =>
        jobs.Where(j =>
            {
                //Filter jobs with busy connectors
                if (GetJobConnector(j) is { } mangaConnector)
                    return busyConnectors.Contains(mangaConnector) == false;
                return true;
            }).ToList();

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