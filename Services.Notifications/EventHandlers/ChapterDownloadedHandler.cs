using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions.Data;
using RabbitMQ.Client;
using Services.Notifications.Database;
using Services.Notifications.Helpers;

namespace Services.Notifications.EventHandlers;

internal sealed class ChapterDownloadedHandler(IChannel channel, IServiceProvider serviceProvider) : TrangaEventHandler<ChapterDownloadedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(ChapterDownloadedEvent notificationEvent)
    {
        string? volume = $"Vol. {notificationEvent.Volume}";
        string? chapter = $"Ch. {notificationEvent.Chapter}";
        string notificationTitle = string.Join(' ', $"Downloaded {notificationEvent.Series}", volume, chapter);
        Notification notification = new(Title: notificationTitle, Text: notificationEvent.Title);
        return await serviceProvider.CreateScope().ServiceProvider.GetRequiredService<NotificationsContext>().NotificationExtensions
            .SendNotifications(notification, CancellationToken.None);
    }
}