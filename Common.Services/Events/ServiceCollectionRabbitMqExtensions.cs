using RabbitMQ.Client;

namespace Common.Services.Events;

/// <summary>Registers a RabbitMQ connection, channel, and the shared "tranga" exchange in the DI container.</summary>
public static class ServiceCollectionRabbitMqExtensions
{
    /// <summary>Connects to the RabbitMQ broker, declares the "tranga" direct exchange, and registers the connection manager, connection, and channel as singletons.</summary>
    internal static IServiceCollection AddRabbitMq(this IServiceCollection services, string hostName, int port, string userName, string password)
    {
        ConnectionManager connectionManager = new (hostName, port, userName, password);
        services.AddSingleton(connectionManager);

        IConnection connection = connectionManager.GetConnection().Result;
        services.AddSingleton(connection);

        IChannel channel = connection.CreateChannelAsync().Result;
        services.AddSingleton(channel);

        channel.ExchangeDeclareAsync("tranga", "direct");
        
        return services;
    }
}