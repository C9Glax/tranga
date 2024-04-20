using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2ConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, _parent.GetConnectors());
    }

    private ValueTuple<HttpStatusCode, object?> GetV2ConnectorConnectorNameGetManga(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 || !_parent.GetConnectors().Contains(groups[1].Value))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, $"Connector '{groups[1].Value}' does not exist.");
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
}