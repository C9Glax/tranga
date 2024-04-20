using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2LogFile(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2Ping(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, "Pong!");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2Ping(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, "Pong!");
    }
}