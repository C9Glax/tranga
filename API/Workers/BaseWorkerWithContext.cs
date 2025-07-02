using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public abstract class BaseWorkerWithContext<T>(IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null) : BaseWorker(dependsOn) where T : DbContext
{
    protected T DbContext { get; init; } = scope.ServiceProvider.GetRequiredService<T>();
}