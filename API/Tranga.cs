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
            List<Job> completedJobs = context.Jobs.Where(j => j.state >= JobState.Completed).ToList();
            Log.Debug($"Completed jobs: {completedJobs.Count}");
            foreach (Job job in completedJobs)
                if (job.RecurrenceMs <= 0)
                    context.Jobs.Remove(job);
                else
                {
                    if (job.state >= JobState.Failed)
                        job.Enabled = false;
                    else
                        job.state = JobState.Waiting;
                    job.LastExecution = DateTime.UtcNow;
                }

            List<Job> runJobs = context.Jobs.Where(j => j.state <= JobState.Running && j.Enabled == true).ToList()
                .Where(j => j.NextExecution < DateTime.UtcNow).ToList();
            Log.Debug($"Due jobs: {runJobs.Count}");
            Log.Debug($"Running jobs: {RunningJobs.Count}");
            IEnumerable<Job> orderedJobs = OrderJobs(runJobs, context).ToList();
            Log.Debug($"Ordered jobs: {orderedJobs.Count()}");
            foreach (Job job in orderedJobs)
            {
                // If the job is already running, skip it
                if (RunningJobs.Values.Any(j => j.JobId == job.JobId)) continue;

                //If a Job for that connector is already running, skip it
                if (job is DownloadAvailableChaptersJob dncj)
                {
                    if (RunningJobs.Values.Any(j =>
                            j is DownloadAvailableChaptersJob rdncj && 
                            context.Mangas.Find(rdncj.MangaId)?.MangaConnector == context.Mangas.Find(dncj.MangaId)?.MangaConnector))
                    {
                        continue;
                    }
                }
                else if (job is DownloadSingleChapterJob dscj)
                {
                    if (RunningJobs.Values.Any(j =>
                            j is DownloadSingleChapterJob rdscj &&
                            context.Chapters.Find(rdscj.ChapterId)?.ParentManga?.MangaConnector ==
                            context.Chapters.Find(dscj.ChapterId)?.ParentManga?.MangaConnector))
                    {
                        continue;
                    }
                }

                Thread t = new(() =>
                {
                    job.Run(serviceProvider);
                });
                RunningJobs.Add(t, job);
                t.Start();
            }

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

    private static IEnumerable<Job> OrderJobs(List<Job> jobs, PgsqlContext context)
    {
        Dictionary<JobType, List<Job>> jobsByType = new();
        foreach (Job job in jobs)
            if(!jobsByType.TryAdd(job.JobType, [job]))
                jobsByType[job.JobType].Add(job);

        IEnumerable<Job> ret = new List<Job>();
        if(jobsByType.ContainsKey(JobType.MoveMangaLibraryJob))
            ret = ret.Concat(jobsByType[JobType.MoveMangaLibraryJob]);
        if(jobsByType.ContainsKey(JobType.MoveFileOrFolderJob))
            ret = ret.Concat(jobsByType[JobType.MoveFileOrFolderJob]);
        if(jobsByType.ContainsKey(JobType.DownloadMangaCoverJob)) 
            ret = ret.Concat(jobsByType[JobType.DownloadMangaCoverJob]);
        if(jobsByType.ContainsKey(JobType.UpdateFilesDownloadedJob))
            ret = ret.Concat(jobsByType[JobType.UpdateFilesDownloadedJob]);

        Dictionary<MangaConnector, List<Job>> metadataJobsByConnector = new();
        if (jobsByType.ContainsKey(JobType.DownloadAvailableChaptersJob))
        {
            foreach (DownloadAvailableChaptersJob job in jobsByType[JobType.DownloadAvailableChaptersJob])
            {
                Manga? manga = context.Mangas.Find(job.MangaId);
                if(manga is null)
                    continue;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
                if(!metadataJobsByConnector.TryAdd(connector, [job]))
                    metadataJobsByConnector[connector].Add(job);
            }
        }
        if (jobsByType.ContainsKey(JobType.UpdateMetaDataJob))
        {
            foreach (UpdateMetadataJob job in jobsByType[JobType.UpdateMetaDataJob])
            {
                Manga manga = job.Manga ?? context.Mangas.Find(job.MangaId)!;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
                if(!metadataJobsByConnector.TryAdd(connector, [job]))
                    metadataJobsByConnector[connector].Add(job);
            }
        }
        if (jobsByType.ContainsKey(JobType.RetrieveChaptersJob))
        {
            foreach (RetrieveChaptersJob job in jobsByType[JobType.RetrieveChaptersJob])
            {
                Manga? manga = context.Mangas.Find(job.MangaId);
                if(manga is null)
                    continue;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
                if(!metadataJobsByConnector.TryAdd(connector, [job]))
                    metadataJobsByConnector[connector].Add(job);
            }
        }
        foreach (List<Job> metadataJobs in metadataJobsByConnector.Values)
            ret = ret.Append(metadataJobs.MinBy(j => j.NextExecution))!;

        if (jobsByType.ContainsKey(JobType.DownloadSingleChapterJob))
        {
            
            Dictionary<MangaConnector, List<DownloadSingleChapterJob>> downloadJobsByConnector = new();
            foreach (DownloadSingleChapterJob job in jobsByType[JobType.DownloadSingleChapterJob])
            {
                Chapter? chapter = context.Chapters.Find(job.ChapterId);
                if(chapter is null)
                    continue;
                Manga manga = chapter.ParentManga ?? context.Mangas.Find(chapter.ParentMangaId)!;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
            
                if(!downloadJobsByConnector.TryAdd(connector, [job]))
                    downloadJobsByConnector[connector].Add(job);
            }
            //From all jobs select those that are supposed to be executed soonest, then select the minimum chapternumber
            foreach (List<DownloadSingleChapterJob> downloadJobs in downloadJobsByConnector.Values)
                ret = ret.Append(
                    downloadJobs.Where(j => j.NextExecution == downloadJobs
                            .MinBy(mj => mj.NextExecution)!.NextExecution)
                        .MinBy(j => context.Chapters.Find(j.ChapterId)!))!;
        }
        
        return ret;
    }
}