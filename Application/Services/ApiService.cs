namespace Application.Services;

public sealed class ApiService : BackgroundService
{
    private readonly API.API _api = new ([]);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _api.Run(stoppingToken);
    }
}