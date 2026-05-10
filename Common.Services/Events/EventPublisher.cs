using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Common.Services.Events;

public sealed class EventPublisher(IChannel channel)
{
    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : TrangaEvent
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        if (channel is { IsOpen: true })
            await channel.BasicPublishAsync("tranga", typeof(T).Name, body, ct);
        else 
            throw new Exception($"Connection is closed: {channel.CloseReason}");
    }
}