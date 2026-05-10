using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Services.Events;

public abstract class TrangaEventHandler<T> : IEventHandler where T : TrangaEvent 
{
    private readonly IChannel _channel;

    private readonly AsyncEventingBasicConsumer _consumer;

    private readonly string _queue = typeof(T).Name;

    protected TrangaEventHandler([FromServices]IChannel channel)
    {
        this._channel = channel;
        channel.QueueDeclareAsync(typeof(T).Name);
        channel.QueueBindAsync(typeof(T).Name, "tranga", typeof(T).Name);
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

    // Abstract method for handling a message.
    // Must be implemented in derived consumers (e.g., PaymentConsumer, InventoryConsumer).
    protected abstract Task<bool> HandleMessage(T message);
}