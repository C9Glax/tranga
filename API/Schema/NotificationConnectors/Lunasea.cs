namespace API.Schema.NotificationConnectors;

public class Lunasea(string id)
    : NotificationConnector(TokenGen.CreateToken(typeof(Lunasea), 64), NotificationConnectorType.LunaSea)
{
    public string Id { get; init; } = id;
}