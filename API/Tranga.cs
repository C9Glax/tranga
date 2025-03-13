using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using API.Schema.NotificationConnectors;
using log4net;
using log4net.Config;

namespace API;

public static class Tranga
{
    public static Thread NotificationSenderThread { get; } = new (NotificationSender);
    public static Thread JobStarterThread { get; } = new (JobStarter);
    private static readonly Dictionary<Thread, Job> RunningJobs = new();
    private static readonly ILog Log = LogManager.GetLogger(typeof(Tranga));

    internal static void StartLogger()
    {
        BasicConfigurator.Configure();
    }

    private static void NotificationSender(object? pgsqlContext)
    {
        if(pgsqlContext is null) return;
        PgsqlContext context = (PgsqlContext)pgsqlContext;

        IQueryable<Notification> staleNotifications = context.Notifications.Where(n => n.Urgency < NotificationUrgency.Normal);
        context.Notifications.RemoveRange(staleNotifications);
        context.SaveChanges();
        while (true)
        {
            SendNotifications(context, NotificationUrgency.High);
            SendNotifications(context, NotificationUrgency.Normal);
            SendNotifications(context, NotificationUrgency.Low);
            
            context.SaveChanges();
            Thread.Sleep(2000);
        }
    }

    private static void SendNotifications(PgsqlContext context, NotificationUrgency urgency)
    {
        List<Notification> notifications = context.Notifications.Where(n => n.Urgency == urgency).ToList();
        if (notifications.Any())
        {
            DateTime max = notifications.MaxBy(n => n.Date)!.Date;
            if (DateTime.UtcNow.Subtract(max) > TrangaSettings.NotificationUrgencyDelay(urgency))
            {
                foreach (NotificationConnector notificationConnector in context.NotificationConnectors)
                {
                    foreach (Notification notification in notifications)
                        notificationConnector.SendNotification(notification.Title, notification.Message);
                }
                context.Notifications.RemoveRange(notifications);
            }
        }
        context.SaveChanges();
    }

    private static void JobStarter(object? serviceProviderObj)
    {
        if(serviceProviderObj is null) return;
        IServiceProvider serviceProvider = (IServiceProvider)serviceProviderObj;
        using IServiceScope scope = serviceProvider.CreateScope();
        PgsqlContext? context = scope.ServiceProvider.GetService<PgsqlContext>();
        if (context is null) return;

        string TRANGA =
            "\n\n _______                                   \n|_     _|.----..---.-..-----..-----..---.-.\n  |   |  |   _||  _  ||     ||  _  ||  _  |\n  |___|  |__|  |___._||__|__||___  ||___._|\n                             |_____|       \n\n";
        Log.Info(TRANGA);
        while (true)
        {
            List<Job> completedJobs = context.Jobs.Where(j => j.state >= JobState.Completed).ToList();
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
                    context.Jobs.Update(job);
                }

            List<Job> runJobs = context.Jobs.Where(j => j.state <= JobState.Running && j.Enabled == true).ToList()
                .Where(j => j.NextExecution < DateTime.UtcNow).ToList();
            foreach (Job job in OrderJobs(runJobs, context))
            {
                // If the job is already running, skip it
                if (RunningJobs.Values.Any(j => j.JobId == job.JobId)) continue;

                if (job is DownloadAvailableChaptersJob dncj)
                {
                    if (RunningJobs.Values.Any(j =>
                            j is DownloadAvailableChaptersJob rdncj &&
                            rdncj.Manga?.MangaConnector == dncj.Manga?.MangaConnector))
                    {
                        continue;
                    }
                }
                else if (job is DownloadSingleChapterJob dscj)
                {
                    if (RunningJobs.Values.Any(j =>
                            j is DownloadSingleChapterJob rdscj && rdscj.Chapter?.ParentManga?.MangaConnector ==
                            dscj.Chapter?.ParentManga?.MangaConnector))
                    {
                        continue;
                    }
                }

                Thread t = new(() =>
                {
                    IEnumerable<Job> newJobs = job.Run(serviceProvider);
                });
                RunningJobs.Add(t, job);
                t.Start();
                context.Jobs.Update(job);
            }

            (Thread, Job)[] removeFromThreadsList = RunningJobs.Where(t => !t.Key.IsAlive)
                .Select(t => (t.Key, t.Value)).ToArray();
            foreach ((Thread thread, Job job) thread in removeFromThreadsList)
            {
                RunningJobs.Remove(thread.thread);
                context.Jobs.Update(thread.job);
            }

            context.SaveChanges();
            Thread.Sleep(2000);
        }
    }

    private static IEnumerable<Job> OrderJobs(List<Job> jobs, PgsqlContext context)
    {
        Dictionary<JobType, List<Job>> jobsByType = new();
        foreach (Job job in jobs)
            if(!jobsByType.TryAdd(job.JobType, [job]))
                jobsByType[job.JobType].Add(job);

        IEnumerable<Job> ret = new List<Job>();
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
                Manga manga = job.Manga ?? context.Manga.Find(job.MangaId)!;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
                if(!metadataJobsByConnector.TryAdd(connector, [job]))
                    metadataJobsByConnector[connector].Add(job);
            }
        }
        if (jobsByType.ContainsKey(JobType.UpdateMetaDataJob))
        {
            foreach (UpdateMetadataJob job in jobsByType[JobType.UpdateMetaDataJob])
            {
                Manga manga = job.Manga ?? context.Manga.Find(job.MangaId)!;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
                if(!metadataJobsByConnector.TryAdd(connector, [job]))
                    metadataJobsByConnector[connector].Add(job);
            }
        }
        if (jobsByType.ContainsKey(JobType.RetrieveChaptersJob))
        {
            foreach (RetrieveChaptersJob job in jobsByType[JobType.RetrieveChaptersJob])
            {
                Manga manga = job.Manga ?? context.Manga.Find(job.MangaId)!;
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
                Chapter chapter = job.Chapter ?? context.Chapters.Find(job.ChapterId)!;
                Manga manga = chapter.ParentManga ?? context.Manga.Find(chapter.ParentMangaId)!;
                MangaConnector connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId)!;
            
                if(!downloadJobsByConnector.TryAdd(connector, [job]))
                    downloadJobsByConnector[connector].Add(job);
            }
            //From all jobs select those that are supposed to be executed soonest, then select the minimum chapternumber
            foreach (List<DownloadSingleChapterJob> downloadJobs in downloadJobsByConnector.Values)
                ret = ret.Append(
                    downloadJobs.Where(j => j.NextExecution == downloadJobs
                            .MinBy(mj => mj.NextExecution)!.NextExecution)
                        .MinBy(j => j.Chapter ?? context.Chapters.Find(j.ChapterId)!))!;
        }
        
        return ret;
    }
}