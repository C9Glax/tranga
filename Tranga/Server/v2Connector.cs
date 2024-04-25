using System.Net;
using System.Text.RegularExpressions;
using Tranga.MangaConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2ConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, _parent.GetConnectors());
    }

    private ValueTuple<HttpStatusCode, object?> GetV2ConnectorConnectorNameGetManga(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.GetConnectors().Contains(groups[1].Value) ||
           !_parent.TryGetConnector(groups[1].Value, out MangaConnector? connector) ||
           connector is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, $"Connector '{groups[1].Value}' does not exist.");

        if (requestParameters.TryGetValue("title", out string? title))
        {
            return (HttpStatusCode.OK, connector.GetManga(title));
        }else if (requestParameters.TryGetValue("url", out string? url))
        {
            return (HttpStatusCode.OK, connector.GetMangaFromUrl(url));
        }else
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, "Parameter 'title' or 'url' has to be set.");
    }
}