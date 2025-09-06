using System.Configuration;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public abstract class BaseWorkerWithContext<T>(IEnumerable<BaseWorker>? dependsOn = null) : BaseWorker(dependsOn) where T : DbContext
{
    protected T DbContext = null!;
    private IServiceScope? _scope;

    public void SetScope(IServiceScope scope)
    {
        this._scope = scope;
        this.DbContext = scope.ServiceProvider.GetRequiredService<T>();
    }
    
    /// <exception cref="ConfigurationErrorsException">Scope has not been set. <see cref="SetScope"/></exception>
    public new Task<BaseWorker[]> DoWork()
    {
        if (DbContext is null)
            throw new ConfigurationErrorsException("Scope has not been set.");
        return base.DoWork();
    }
}