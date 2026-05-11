using Extensions;

namespace Services.Notifications.Database;

public record DbNapriseExtension(string Name, string ServiceUrl) : DbNotificationExtension(Name, NotificationExtensionType.Naprise)
{
    public string ServiceUrl { get; init; } = ServiceUrl;
}