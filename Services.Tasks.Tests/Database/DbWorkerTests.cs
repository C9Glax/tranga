using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.Database;

public class DbWorkerTests : TrangaTest
{
    [Fact]
    public void DbWorker_CanBeConstructedWithDefaults()
    {
        DbWorker worker = new();

        Assert.NotEqual(Guid.Empty, worker.WorkerId);
        Assert.Equal(WorkerStatus.Idle, worker.Status);
        Assert.Null(worker.CurrentTaskId);
        Assert.Null(worker.CurrentTaskTypeId);
    }

    [Fact]
    public async Task TasksContext_CanInsertAndRetrieveWorker()
    {
        using TasksContext ctx = TasksContextFactory.Create();
        DbWorker worker = new()
        {
            Status = WorkerStatus.Busy,
            CurrentTaskId = Guid.CreateVersion7(),
            CurrentTaskTypeId = Guid.CreateVersion7()
        };

        await ctx.Workers.AddAsync(worker, ct);
        await ctx.SaveChangesAsync(ct);

        DbWorker? fromDb = await ctx.Workers.SingleOrDefaultAsync(w => w.WorkerId == worker.WorkerId, ct);

        Assert.NotNull(fromDb);
        Assert.Equal(worker.Status, fromDb.Status);
        Assert.Equal(worker.CurrentTaskId, fromDb.CurrentTaskId);
        Assert.Equal(worker.CurrentTaskTypeId, fromDb.CurrentTaskTypeId);
    }

    [Fact]
    public async Task TasksContext_ExecuteDeleteAsync_RemovesAllWorkers()
    {
        using TasksContext ctx = TasksContextFactory.Create();
        await ctx.Workers.AddRangeAsync([new DbWorker(), new DbWorker()], ct);
        await ctx.SaveChangesAsync(ct);

        int removed = await ctx.Workers.ExecuteDeleteAsync(ct);

        Assert.Equal(2, removed);
        Assert.Empty(await ctx.Workers.ToListAsync(ct));
    }
}
