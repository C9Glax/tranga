namespace Application.Services;

public sealed class ApiService : IHostedService
{
    private readonly API.API _api = new ([]);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _api.Run();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _api.DisposeAsync();
    }
}