using Extensions;
using Extensions.Extensions.NaprisExtensions;
using Services.Notifications.Database;

namespace Services.Notifications.Helpers;

internal static class NotificationExtensionExtensions
{
    public static INotificationExtension CreateNotificationExtension(this DbNotificationExtension extension) =>
        extension.Type switch
        {
            NotificationExtensionType.Naprise => new NapriseExtension(((DbNapriseExtension)extension).ServiceUrl),
            NotificationExtensionType.Gotify => new Gotify(((DbNapriseExtension)extension).ServiceUrl),
            NotificationExtensionType.Discord => new Discord(((DbNapriseExtension)extension).ServiceUrl),
            NotificationExtensionType.NtfySh => new NtfySh(((DbNapriseExtension)extension).ServiceUrl),
            NotificationExtensionType.Telegram => new Telegram(((DbNapriseExtension)extension).ServiceUrl),
            _ => throw new NotImplementedException()
        };
}