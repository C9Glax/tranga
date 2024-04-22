using System.Net;
using System.Text.RegularExpressions;
using Tranga.NotificationConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnector(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, notificationConnectors);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK,
            Enum.GetValues<NotificationConnectors.NotificationConnector.NotificationConnectorType>().ToDictionary(b => (byte)b, b => Enum.GetName(b)));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out NotificationConnector.NotificationConnectorType notificationConnectorType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {groups[1].Value} does not exist.");
        }
        
        if(notificationConnectors.All(nc => nc.notificationConnectorType != notificationConnectorType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {Enum.GetName(notificationConnectorType)} not configured.");
        else
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, notificationConnectors.First(nc => nc.notificationConnectorType != notificationConnectorType));
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out NotificationConnector.NotificationConnectorType notificationConnectorType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {groups[1].Value} does not exist.");
        }

        string? url;
        switch (notificationConnectorType)
        {
            case NotificationConnector.NotificationConnectorType.Gotify:
                if(!requestParameters.TryGetValue("url", out url))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");
                if(!requestParameters.TryGetValue("appToken", out string? appToken))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'appToken' missing.");
                Gotify gotify = new (this, url, appToken);
                this.notificationConnectors.RemoveWhere(nc =>
                    nc.notificationConnectorType == NotificationConnector.NotificationConnectorType.Gotify);
                this.notificationConnectors.Add(gotify);
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, gotify);
            case NotificationConnector.NotificationConnectorType.LunaSea:
                if(!requestParameters.TryGetValue("webhook", out string? webhook))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'webhook' missing.");
                LunaSea lunaSea = new (this, webhook);
                this.notificationConnectors.RemoveWhere(nc =>
                    nc.notificationConnectorType == NotificationConnector.NotificationConnectorType.LunaSea);
                this.notificationConnectors.Add(lunaSea);
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, lunaSea);
            case NotificationConnector.NotificationConnectorType.Ntfy:
                if(!requestParameters.TryGetValue("url", out url))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");
                if(!requestParameters.TryGetValue("auth", out string? auth))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'auth' missing.");
                Ntfy ntfy = new(this, url, auth);
                this.notificationConnectors.RemoveWhere(nc =>
                    nc.notificationConnectorType == NotificationConnector.NotificationConnectorType.Ntfy);
                this.notificationConnectors.Add(ntfy);
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, ntfy);
            default:
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, $"NotificationType {Enum.GetName(notificationConnectorType)} is not supported.");
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2NotificationConnectorTypeTest(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out NotificationConnector.NotificationConnectorType notificationConnectorType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {groups[1].Value} does not exist.");
        }

        string? url;
        switch (notificationConnectorType)
        {
            case NotificationConnector.NotificationConnectorType.Gotify:
                if(!requestParameters.TryGetValue("url", out url))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");
                if(!requestParameters.TryGetValue("appToken", out string? appToken))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'appToken' missing.");
                Gotify gotify = new (this, url, appToken);
                gotify.SendNotification("Tranga Test", "It was successful :3");
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, gotify);
            case NotificationConnector.NotificationConnectorType.LunaSea:
                if(!requestParameters.TryGetValue("webhook", out string? webhook))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'webhook' missing.");
                LunaSea lunaSea = new (this, webhook);
                lunaSea.SendNotification("Tranga Test", "It was successful :3");
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, lunaSea);
            case NotificationConnector.NotificationConnectorType.Ntfy:
                if(!requestParameters.TryGetValue("url", out url))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");
                if(!requestParameters.TryGetValue("auth", out string? auth))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'auth' missing.");
                Ntfy ntfy = new(this, url, auth);
                ntfy.SendNotification("Tranga Test", "It was successful :3");
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, ntfy);
            default:
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, $"NotificationType {Enum.GetName(notificationConnectorType)} is not supported.");
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out NotificationConnector.NotificationConnectorType notificationConnectorType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {groups[1].Value} does not exist.");
        }
        
        if(notificationConnectors.All(nc => nc.notificationConnectorType != notificationConnectorType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"NotificationType {Enum.GetName(notificationConnectorType)} not configured.");
        else
        {
            notificationConnectors.Remove(notificationConnectors.First(nc => nc.notificationConnectorType != notificationConnectorType));
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
        }
    }
}