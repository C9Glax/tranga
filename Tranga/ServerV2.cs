using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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
        
        
        switch (request.HttpMethod)
        {
            case "GET":
                HandleGetV2(path, response, requestParams);
                break;
            case "POST":
                HandlePostV2(path, response, requestParams);
                break;
            case "DELETE":
                HandleDeleteV2(path, response, requestParams);
                break;
            default: 
                SendResponse(HttpStatusCode.MethodNotAllowed, response);
                break;
        }
    }
    
    private Dictionary<string, string> GetRequestBody(HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
        {
            Log("No request body");
            return new Dictionary<string, string>();
        }
        Stream body = request.InputStream;
        Encoding encoding = request.ContentEncoding;
        using StreamReader streamReader = new (body, encoding);
        try
        {
            Dictionary<string, string> requestBody =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd())
                ?? new();
            return requestBody;
        }
        catch (JsonException e)
        {
            Log(e.Message);
        }
        return new Dictionary<string, string>();
    }
    
    private void HandleGetV2(string path, HttpListenerResponse response,
        Dictionary<string, string> requestParameters)
    {
        throw new NotImplementedException("v2 not implemented yet");
    }
    
    private void HandlePostV2(string path, HttpListenerResponse response,
        Dictionary<string, string> requestParameters)
    {
        throw new NotImplementedException("v2 not implemented yet");
    }
    
    private void HandleDeleteV2(string path, HttpListenerResponse response,
        Dictionary<string, string> requestParameters)
    {
        throw new NotImplementedException("v2 not implemented yet");
    }
}