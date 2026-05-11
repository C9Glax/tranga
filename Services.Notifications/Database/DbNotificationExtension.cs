using Extensions;

namespace Services.Notifications.Database;

public abstract record DbNotificationExtension(string Name, NotificationExtensionType Type)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public NotificationExtensionType Type { get; init; } = Type;

    public string Name { get; init; } = Name;
}