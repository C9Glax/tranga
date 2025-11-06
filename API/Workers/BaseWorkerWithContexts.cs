using API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public abstract class BaseWorkerWithContexts(IEnumerable<BaseWorker>? dependsOn = null) : BaseWorker(dependsOn)
{
    /// <summary>
    /// Returns the context of requested type <typeparamref name="T"/>
    /// </summary>
    /// <param name="scope"></param>
    /// <typeparam name="T">Type of <see cref="DbContext"/></typeparam>
    /// <returns>Context in scope</returns>
    /// <exception cref="Exception">Scope not set</exception>
    protected static T GetContext<T>(IServiceScope scope) where T : DbContext
    {
        if (scope is not { } serviceScope)
            throw new ScopeNotSetException();
        return serviceScope.ServiceProvider.GetRequiredService<T>();
    }

    protected abstract void SetContexts(IServiceScope serviceScope);
    
    public new Task<BaseWorker[]> DoWork(IServiceScope serviceScope, Action? callback = null)
    {
        SetContexts(serviceScope);
        return base.DoWork(callback);
    }
}