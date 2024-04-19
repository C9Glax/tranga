using System.Net;
using System.Text.RegularExpressions;

namespace Tranga;

public partial class Server
{
    private void HandleRequestV2(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);  //Variables in the URI
        Dictionary<string, string> requestBody = GetRequestBody(request);                       //Variables in the JSON body
        Dictionary<string, string> requestParams = requestVariables.UnionBy(requestBody, v => v.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value); //The actual variable used for the API

        ValueTuple<HttpStatusCode, object?> responseMessage = request.HttpMethod switch
        {
            "GET" => HandleGetV2(path, requestParams),
            "POST" => HandlePostV2(path, requestParams),
            "DELETE" => HandleDeleteV2(path, requestParams),
            _ => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, null)
        };
        
        SendResponse(responseMessage.Item1, response, responseMessage.Item2);
    }
    
    private ValueTuple<HttpStatusCode, object?> HandleGetV2(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }
    
    private ValueTuple<HttpStatusCode, object?> HandlePostV2(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }
    
    private ValueTuple<HttpStatusCode, object?> HandleDeleteV2(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }
}