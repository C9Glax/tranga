using RabbitMQ.Client;

namespace Common.Services.Events;

public static class ServiceCollectionRabbitMqExtensions
{
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