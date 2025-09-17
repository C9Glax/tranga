using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

public class UpdateLibraryConnectorsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<LibraryContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMinutes(10);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        List<LibraryConnector> connectors = await DbContext.LibraryConnectors.ToListAsync(CancellationToken);
        foreach (LibraryConnector libraryConnector in connectors)
            await libraryConnector.UpdateLibrary(CancellationToken);
        return [];
    }
}