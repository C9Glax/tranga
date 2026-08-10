using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Common.Services.Events;

/// <summary>Publishes <see cref="TrangaEvent"/> messages to the shared "tranga" RabbitMQ exchange.</summary>
/// <param name="channel">The open RabbitMQ channel to publish on.</param>
public sealed class EventPublisher(IChannel channel)
{
    /// <summary>Serializes <paramref name="message"/> as JSON and publishes it to the "tranga" exchange, routed by the message's type name.</summary>
    /// <typeparam name="T">The concrete event type being published.</typeparam>
    /// <param name="message">The event payload to publish.</param>
    /// <param name="ct">Token to cancel the publish operation.</param>
    /// <exception cref="Exception">Thrown when the underlying RabbitMQ channel is not open.</exception>
    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : TrangaEvent
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        if (channel is { IsOpen: true })
            await channel.BasicPublishAsync("tranga", typeof(T).Name, body, ct);
        else 
            throw new Exception($"Connection is closed: {channel.CloseReason}");
    }
}