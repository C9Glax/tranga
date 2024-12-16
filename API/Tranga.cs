﻿using API.Schema;
using API.Schema.Jobs;
using API.Schema.NotificationConnectors;
using log4net;
using log4net.Config;

namespace API;

public static class Tranga
{
    public static Thread NotificationSenderThread { get; } = new (NotificationSender);
    public static Thread JobStarterThread { get; } = new (JobStarter);
    private static readonly List<Thread> RunningJobs = new();
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
            if (DateTime.Now.Subtract(max) > TrangaSettings.NotificationUrgencyDelay(urgency))
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

    private static void JobStarter(object? pgsqlContext)
    {
        if(pgsqlContext is null) return;
        PgsqlContext context = (PgsqlContext)pgsqlContext;
        
        string TRANGA = "\n\n _______                                   \n|_     _|.----..---.-..-----..-----..---.-.\n  |   |  |   _||  _  ||     ||  _  ||  _  |\n  |___|  |__|  |___._||__|__||___  ||___._|\n                             |_____|       \n\n";
        Log.Info(TRANGA);
        while (true)
        {
            List<Job> completedJobs = context.Jobs.Where(j => j.state == JobState.Completed).ToList();
            foreach (Job job in completedJobs)
                if(job.RecurrenceMs < 1)
                    context.Jobs.Remove(job);
                else
                {
                    job.LastExecution = DateTime.UtcNow;
                    job.state = JobState.Waiting;
                    context.Jobs.Update(job);
                }
            
            List<Job> runJobs = context.Jobs.Where(j => j.state <= JobState.Running && j.NextExecution < DateTime.UtcNow).ToList();
            foreach (Job job in runJobs)
            {
                Thread t = new (() =>
                {
                    IEnumerable<Job> newJobs = job.Run();
                    context.Jobs.AddRange(newJobs);
                });
                RunningJobs.Add(t);
                t.Start();
                context.Jobs.Update(job);
            }
            
            Thread[] removeFromThreadsList = RunningJobs.Where(t => !t.IsAlive).ToArray();
            foreach (Thread thread in removeFromThreadsList)
                RunningJobs.Remove(thread);
            
            context.SaveChanges();
            Thread.Sleep(2000);
        }
    }
}