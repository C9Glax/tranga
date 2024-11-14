namespace API.Schema.NotificationConnectors;

public class Ntfy(string endpoint, string auth, string topic)
    : NotificationConnector(TokenGen.CreateToken(typeof(Ntfy), 64), NotificationConnectorType.Ntfy)
{
    public string Endpoint { get; init; } = endpoint;
    public string Auth { get; init; } = auth;
    public string Topic { get; init; } = topic;
}