using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnector(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2NotificationConnectorTypeTest(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2NotificationConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
}