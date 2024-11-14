using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class Ntfy(API.Schema.NotificationConnectors.Ntfy info) : NotificationConnector(info)
{
    public static string AuthFromUsernamePassword(string username, string password)
    {
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        string authParam = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        return authParam;
    }

    public static string[] EndpointAndTopicFromUrl(string url)
    {
        string[] ret = new string[2];
        if (!BaseUrlRex.IsMatch(url))
            throw new ArgumentException("url does not match pattern");
        Regex rootUriRex = new(@"(https?:\/\/[a-zA-Z0-9-\.]+\.[a-zA-Z0-9]+)(?:\/([a-zA-Z0-9-\.]+))?.*");
        Match match = rootUriRex.Match(url);
        if(!match.Success)
            throw new ArgumentException($"Error getting URI from provided endpoint-URI: {url}");
        
        ret[0] = match.Groups[1].Value;
        ret[1] = match.Groups[2].Success && match.Groups[2].Value.Length > 0 ? match.Groups[2].Value : "tranga";

        return ret;
    }

    public override void SendNotification(string title, string notificationText)
    {
        API.Schema.NotificationConnectors.Ntfy i = (API.Schema.NotificationConnectors.Ntfy)info;
        
        log.Info($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, i.Topic, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{i.Endpoint}?auth={i.Auth}");
        request.Content = new StringContent(JsonConvert.SerializeObject(message, Formatting.None), Encoding.UTF8, "application/json");
        HttpResponseMessage response = _client.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            StreamReader sr = new (response.Content.ReadAsStream());
            log.Info($"{response.StatusCode}: {sr.ReadToEnd()}");
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