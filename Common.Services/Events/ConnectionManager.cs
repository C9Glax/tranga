using RabbitMQ.Client;

namespace Common.Services.Events;

internal sealed class ConnectionManager(string hostName, int port, string userName, string password)
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = hostName,
        UserName = userName,
        Password = password,
        Port = port
    };

    private IConnection? _connection;

    public async Task<IConnection> GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection = await _factory.CreateConnectionAsync();
        return _connection;
    }
}