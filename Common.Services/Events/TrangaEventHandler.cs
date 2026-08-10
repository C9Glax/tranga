using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Services.Events;

/// <summary>
/// Base class for consuming a specific <see cref="TrangaEvent"/> type from RabbitMQ. Declares and binds a durable
/// queue named after <typeparamref name="T"/> to the "tranga" exchange, and dispatches received messages to
/// <see cref="HandleMessage"/>, acking on success and nacking (without requeue) on failure or deserialization error.
/// </summary>
/// <typeparam name="T">The event type this handler consumes.</typeparam>
public abstract class TrangaEventHandler<T> : IEventHandler where T : TrangaEvent
{
    private readonly IChannel _channel;

    private readonly AsyncEventingBasicConsumer _consumer;

    private readonly string _queue = typeof(T).Name;

    /// <summary>Declares and binds the queue for <typeparamref name="T"/> and starts consuming messages from it.</summary>
    /// <param name="channel">The RabbitMQ channel to consume on.</param>
    protected TrangaEventHandler([FromServices]IChannel channel)
    {
        this._channel = channel;
        channel.QueueDeclareAsync(queue: typeof(T).Name, durable: true, exclusive: false, autoDelete: true);
        channel.QueueBindAsync(queue: typeof(T).Name, exchange: "tranga", routingKey: typeof(T).Name);
        _consumer = new(channel);

        _consumer.ReceivedAsync += ConsumerOnReceivedAsync;

        channel.BasicConsumeAsync(_queue, autoAck: false, _consumer).Wait();
    }

    private async Task ConsumerOnReceivedAsync(object _, BasicDeliverEventArgs ea)
    {
        try
        {
            string bodyJson = Encoding.UTF8.GetString(ea.Body.ToArray());

            if (JsonSerializer.Deserialize<T>(bodyJson) is not { } message || !await HandleMessage(message))
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
            else
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        }
        catch (Exception)
        {
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    /// <summary>Processes a received event.</summary>
    /// <param name="notificationEvent">The deserialized event message.</param>
    /// <returns><c>true</c> if the message was handled successfully and should be acknowledged; <c>false</c> if it should be nacked (without requeue).</returns>
    protected abstract Task<bool> HandleMessage(T notificationEvent);
}