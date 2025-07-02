using API.Schema.NotificationsContext;

namespace API.Workers;

public class SendNotificationsWorker(IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<NotificationsContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    protected override BaseWorker[] DoWorkInternal()
    {
        throw new NotImplementedException();
    }

}