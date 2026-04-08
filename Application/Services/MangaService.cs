using Services.Manga;

namespace Application.Services;

public sealed class MangaService : IHostedService
{
    private readonly Service _service = new ([]);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _service.Run();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _service.DisposeAsync();
    }
}