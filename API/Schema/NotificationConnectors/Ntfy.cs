using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace API.Schema.NotificationConnectors;

public class Ntfy : NotificationConnector
{
    private NotificationConnector _notificationConnectorImplementation;
    public string Endpoint { get; init; }
    public string Auth { get; init; }
    public string Topic { get; init; }

    public Ntfy(string endpoint, string auth, string topic): base(TokenGen.CreateToken(typeof(Ntfy), 64), NotificationConnectorType.Ntfy)
    {
        Endpoint = endpoint;
        Auth = auth;
        Topic = topic;
    }
    
    public Ntfy(string endpoint, string username, string password, string? topic = null) : 
        this(EndpointAndTopicFromUrl(endpoint)[0], topic??EndpointAndTopicFromUrl(endpoint)[1], AuthFromUsernamePassword(username, password))
    {
        
    }

    private static string AuthFromUsernamePassword(string username, string password)
    {
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        string authParam = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        return authParam;
    }

    private static string[] EndpointAndTopicFromUrl(string url)
    {
        string[] ret = new string[2];
        Regex rootUriRex = new(@"(https?:\/\/[a-zA-Z0-9-\.]+\.[a-zA-Z0-9]+)(?:\/([a-zA-Z0-9-\.]+))?.*");
        Match match = rootUriRex.Match(url);
        if(!match.Success)
            throw new ArgumentException($"Error getting URI from provided endpoint-URI: {url}");
        
        ret[0] = match.Groups[1].Value;
        ret[1] = match.Groups[2].Success && match.Groups[2].Value.Length > 0 ? match.Groups[2].Value : "tranga";

        return ret;
    }
    
    protected override void SendNotificationInternal(string title, string notificationText)
    {
        MessageData message = new(title, Topic, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{this.Endpoint}?auth={this.Auth}");
        request.Content = new StringContent(JsonConvert.SerializeObject(message, Formatting.None), Encoding.UTF8, "application/json");
        HttpResponseMessage response = _client.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            StreamReader sr = new (response.Content.ReadAsStream());
            //TODO
        }
    }

    private class MessageData
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string topic { get; }
        public string title { get; }
        public string message { get; }
        public int priority { get; }

        public MessageData(string title, string topic, string message)
        {
            this.topic = topic;
            this.title = title;
            this.message = message;
            this.priority = 3;
        }
    }
}