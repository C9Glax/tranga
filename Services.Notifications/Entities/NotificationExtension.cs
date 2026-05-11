using Extensions;
using Services.Notifications.Database;

namespace Services.Notifications.Entities;

public sealed record NotificationExtension
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required NotificationExtensionType Type { get; init; }
}