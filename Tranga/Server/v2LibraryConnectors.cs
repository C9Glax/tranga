using System.Net;
using System.Text.RegularExpressions;
using Tranga.LibraryConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnector(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, libraryConnectors);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnectorTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK,
            Enum.GetValues<LibraryConnector.LibraryType>().ToDictionary(b => (byte)b, b => Enum.GetName(b)));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out LibraryConnector.LibraryType libraryType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {groups[1].Value} does not exist.");
        }
        
        if(libraryConnectors.All(lc => lc.libraryType != libraryType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {Enum.GetName(libraryType)} not configured.");
        else
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, libraryConnectors.First(lc => lc.libraryType == libraryType));
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out LibraryConnector.LibraryType libraryType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {groups[1].Value} does not exist.");
        }
        
        if(!requestParameters.TryGetValue("URL", out string? url))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");

        switch (libraryType)
        {
            case LibraryConnector.LibraryType.Kavita:
                if(!requestParameters.TryGetValue("username", out string? username))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'username' missing.");
                if(!requestParameters.TryGetValue("password", out string? password))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'password' missing.");
                Kavita kavita = new (this, url, username, password);
                libraryConnectors.RemoveWhere(lc => lc.libraryType == LibraryConnector.LibraryType.Kavita);
                libraryConnectors.Add(kavita);
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, kavita);
            case LibraryConnector.LibraryType.Komga:
                if(!requestParameters.TryGetValue("auth", out string? auth))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'auth' missing.");
                Komga komga = new (this, url, auth);
                libraryConnectors.RemoveWhere(lc => lc.libraryType == LibraryConnector.LibraryType.Komga);
                libraryConnectors.Add(komga);
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, komga);
            default: return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, $"LibraryType {Enum.GetName(libraryType)} is not supported.");
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2LibraryConnectorTypeTest(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out LibraryConnector.LibraryType libraryType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {groups[1].Value} does not exist.");
        }
        
        if(!requestParameters.TryGetValue("URL", out string? url))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'url' missing.");

        switch (libraryType)
        {
            case LibraryConnector.LibraryType.Kavita:
                if(!requestParameters.TryGetValue("username", out string? username))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'username' missing.");
                if(!requestParameters.TryGetValue("password", out string? password))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'password' missing.");
                Kavita kavita = new (this, url, username, password);
                return kavita.Test() switch
                {
                    true => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, kavita),
                    _ => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.FailedDependency, kavita)
                };
            case LibraryConnector.LibraryType.Komga:
                if(!requestParameters.TryGetValue("auth", out string? auth))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotAcceptable, "Parameter 'auth' missing.");
                Komga komga = new (this, url, auth);
                return komga.Test() switch
                {
                    true => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, komga),
                    _ => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.FailedDependency, komga)
                };
            default: return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, $"LibraryType {Enum.GetName(libraryType)} is not supported.");
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2LibraryConnectorType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out LibraryConnector.LibraryType libraryType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {groups[1].Value} does not exist.");
        }
        
        if(libraryConnectors.All(lc => lc.libraryType != libraryType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"LibraryType {Enum.GetName(libraryType)} not configured.");
        else
        {
            libraryConnectors.Remove(libraryConnectors.First(lc => lc.libraryType == libraryType));
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
        }
    }
}