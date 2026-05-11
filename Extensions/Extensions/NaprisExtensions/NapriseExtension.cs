using Extensions.Data;
using Naprise;

namespace Extensions.Extensions.NaprisExtensions;

public class NapriseExtension(string serviceUrl) : INotificationExtension
{
    public string ServiceUrl { get; init; } = serviceUrl;

    private INotifier Notifier { get; init; } = Naprise.Naprise.Create(serviceUrl);

    public Task SendNotification(Notification notification, CancellationToken ct) => Notifier.NotifyAsync(new Message
    {
        Title = notification.Title,
        Markdown = notification.Markdown,
        Text = notification.Text,
        Type = notification.Level switch
        {
            Level.Info => MessageType.Info,
            Level.Error => MessageType.Error,
            Level.Success => MessageType.Success,
            Level.Warning => MessageType.Warning,
            _ => MessageType.None
        }
    }, ct);
}