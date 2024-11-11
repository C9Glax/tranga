namespace API.Schema.NotificationConnectors;

public class Gotify(string endpoint, string appToken)
    : NotificationConnector(TokenGen.CreateToken(typeof(Gotify), 64), Tranga.NotificationConnectors.NotificationConnector.NotificationConnectorType.Gotify)
{
    public string Endpoint { get; init; } = endpoint;
    public string AppToken { get; init; } = appToken;
}