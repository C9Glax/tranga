using Database.MangaContext;
using Npgsql;

namespace Application.Workers;

public class ApplyMigrationsWorker(MangaContext mangaContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await mangaContext.ApplyMigrations();
        }
        catch (NpgsqlException)
        {
            // Unable to connect to DB
        }
    }
}