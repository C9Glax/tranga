using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using API.Schema.MetadataFetchers;
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
    internal static MetadataFetcher[] MetadataFetchers = [new MyAnimeList()];

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
        Log.Info("Logger Configured.");
        Log.Info(TRANGA);
    }

    internal static void RemoveStaleFiles(PgsqlContext context)
    {
        Log.Info("Removing stale files...");
        if (!Directory.Exists(TrangaSettings.coverImageCache))
            return;
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
        
        while (true)
        {            
            Log.Debug("Starting Job-Cycle...");
            DateTime cycleStart = DateTime.UtcNow;
            using IServiceScope scope = serviceProvider.CreateScope();
            PgsqlContext cycleContext = scope.ServiceProvider.GetRequiredService<PgsqlContext>();

            //Get Running Jobs
            List<Job> runningJobs = cycleContext.Jobs.GetRunningJobs();
            
            DateTime filterStart = DateTime.UtcNow;
            Log.Debug("Filtering Jobs...");

            List<Job> waitingJobs = cycleContext.Jobs.GetWaitingJobs();
            List<Job> dueJobs = waitingJobs.FilterDueJobs();
            List<Job> jobsWithoutDependencies = dueJobs.FilterJobDependencies();

            List<Job> startJobs = dueJobs;
            Log.Debug($"Jobs Filtered! (took {DateTime.UtcNow.Subtract(filterStart).TotalMilliseconds}ms)");
            
            
            //Start Jobs that are allowed to run (preconditions match)
            foreach (Job job in startJobs)
            {
                bool running = false;
                Thread t = new(() =>
                {
                    using IServiceScope jobScope = serviceProvider.CreateScope();
                    PgsqlContext jobContext = jobScope.ServiceProvider.GetRequiredService<PgsqlContext>();
                    if (jobContext.Jobs.Find(job.Key) is not { } inContext)
                        return;
                    inContext.Run(jobContext, ref running); //FIND the job IN THE NEW CONTEXT!!!!!!! SO WE DON'T GET TRACKING PROBLEMS AND AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                });
                RunningJobs.Add(t, job);
                t.Start();
                while(!running)
                    Thread.Sleep(10);
            }
            Log.Debug($"Running: {runningJobs.Count}\n" +
                      $"{string.Join("\n", runningJobs.Select(s => "\t- " + s))}\n" +
                      $"Waiting: {waitingJobs.Count} Due: {dueJobs.Count}\n" +
                      $"{string.Join("\n", dueJobs.Select(s => "\t- " + s))}\n" +
                      $"of which {jobsWithoutDependencies.Count} without missing dependencies, of which\n" +
                      $"{startJobs.Count} were started:\n" +
                      $"{string.Join("\n", startJobs.Select(s => "\t- " + s))}");

            if (Log.IsDebugEnabled && dueJobs.Count < 1)
                if(waitingJobs.MinBy(j => j.NextExecution) is { } nextJob)
                    Log.Debug($"Next job in {nextJob.NextExecution.Subtract(DateTime.UtcNow)} (at {nextJob.NextExecution}): {nextJob.Key}");

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
            Log.Debug($"Job-Cycle over! (took {DateTime.UtcNow.Subtract(cycleStart).TotalMilliseconds}ms");
            Thread.Sleep(TrangaSettings.startNewJobTimeoutMs);
        }
    }
    
    private static List<Job> GetRunningJobs(this IQueryable<Job> jobs)
    {
        DateTime start = DateTime.UtcNow;
        List<Job> ret = jobs.Where(j => j.state == JobState.Running).ToList();
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Getting running Jobs took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }
    
    private static List<Job> GetWaitingJobs(this IQueryable<Job> jobs)
    {
        DateTime start = DateTime.UtcNow;
        List<Job> ret = jobs.Where(j => j.state == JobState.CompletedWaiting || j.state == JobState.FirstExecution).ToList();
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Getting waiting Jobs took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }
        

    private static List<Job> FilterDueJobs(this List<Job> jobs)
    {
        DateTime start = DateTime.UtcNow;
        List<Job> ret = jobs.Where(j => j.NextExecution < DateTime.UtcNow).ToList();
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Filtering Due Jobs took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }
        

    private static List<Job> FilterJobDependencies(this List<Job> jobs)
    {
        DateTime start = DateTime.UtcNow;
        List<Job> ret = jobs.Where(job => job.DependsOnJobs.All(j => j.IsCompleted)).ToList();
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Filtering Dependencies took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }
    

    private static List<Job> FilterJobsWithoutDownloading(this List<Job> jobs)
    {
        JobType[] types = [JobType.MoveFileOrFolderJob, JobType.MoveMangaLibraryJob, JobType.UpdateChaptersDownloadedJob];
        DateTime start = DateTime.UtcNow;
        List<Job> ret = jobs.Where(j => types.Contains(j.JobType)).ToList();
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Filtering Jobs without Download took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }

    private static List<Job> MatchJobsRunningAndWaiting(Dictionary<string, Dictionary<JobType, List<Job>>> running,
        Dictionary<string, Dictionary<JobType, List<Job>>> waiting)
    {
        Log.Debug($"Matching {running.Count} running Jobs to {waiting.Count} waiting Jobs. Busy Connectors: {string.Join(", ", running.Select(r => r.Key))}");
        DateTime start = DateTime.UtcNow;
        List<Job> ret = new();
        //Foreach MangaConnector
        foreach ((string connector, Dictionary<JobType, List<Job>> jobTypeJobsWaiting) in waiting)
        {
            //Check if MangaConnector has a Job running
            if (running.TryGetValue(connector, out Dictionary<JobType, List<Job>>? jobTypeJobsRunning))
            {
                //MangaConnector has running Jobs
                //Match per JobType (MangaConnector can have 1 Job per Type running at the same time)
                foreach ((JobType jobType, List<Job> jobsWaiting) in jobTypeJobsWaiting)
                {
                    if(jobTypeJobsRunning.ContainsKey(jobType))
                        //Already a job of Type running on MangaConnector
                        continue;
                    if (jobType is not JobType.DownloadSingleChapterJob)
                        //If it is not a DownloadSingleChapterJob, just add the first
                        ret.Add(jobsWaiting.First());
                    else
                        //Add the Job with the lowest Chapternumber
                        ret.Add(jobsWaiting.OrderBy(j => ((DownloadSingleChapterJob)j).Chapter).First());
                }
            }
            else
            {
                //MangaConnector has no running Jobs
                foreach ((JobType jobType, List<Job> jobsWaiting) in jobTypeJobsWaiting)
                {
                    if(ret.Any(j => j.JobType == jobType))
                        //Already a job of type to be started
                        continue;
                    if (jobType is not JobType.DownloadSingleChapterJob)
                        //If it is not a DownloadSingleChapterJob, just add the first
                        ret.Add(jobsWaiting.First());
                    else
                        //Add the Job with the lowest Chapternumber
                        ret.Add(jobsWaiting.OrderBy(j => ((DownloadSingleChapterJob)j).Chapter).First());
                }
            }
        }
        DateTime end = DateTime.UtcNow;
        Log.Debug($"Getting eligible jobs (not held back by Connector) took {end.Subtract(start).TotalMilliseconds}ms");
        return ret;
    }
}