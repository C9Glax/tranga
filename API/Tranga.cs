using API.Schema;
using API.Schema.Jobs;
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
            List<Job> completedJobs = context.Jobs.Where(j => j.state >= JobState.Completed && j.state < JobState.Failed).ToList();
            foreach (Job job in completedJobs)
                if (job.RecurrenceMs <= 0)
                    context.Jobs.Remove(job);
                else
                {
                    job.LastExecution = DateTime.UtcNow;
                    job.state = JobState.Waiting;
                    context.Jobs.Update(job);
                }

            List<Job> runJobs = context.Jobs.Where(j => j.state <= JobState.Running).ToList()
                .Where(j => j.NextExecution < DateTime.UtcNow).ToList();
            foreach (Job job in runJobs)
            {
                // If the job is already running, skip it
                if (RunningJobs.Values.Any(j => j.JobId == job.JobId)) continue;

                if (job is DownloadNewChaptersJob dncj)
                {
                    if (RunningJobs.Values.Any(j =>
                            j is DownloadNewChaptersJob rdncj &&
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
                    context.Jobs.AddRange(newJobs);
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
}