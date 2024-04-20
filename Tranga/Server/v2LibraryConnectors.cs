using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnector(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2LibraryConnectorTypeTest(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
}