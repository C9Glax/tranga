using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class DownloadProgressHub : Hub
{
    public async Task SubscribeToWorker(string workerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"worker-{workerId}");
    }

    public async Task UnsubscribeFromWorker(string workerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"worker-{workerId}");
    }

    public async Task SubscribeToManga(string mangaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"manga-{mangaId}");
    }

    public async Task UnsubscribeFromManga(string mangaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"manga-{mangaId}");
    }

    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-downloads");
    }

    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-downloads");
    }
}
