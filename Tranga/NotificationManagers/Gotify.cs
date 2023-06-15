using System.Text;
using Logging;
using Newtonsoft.Json;

namespace Tranga.NotificationManagers;

public class Gotify : NotificationManager
{
    public string endpoint { get; }
    public string appToken { get; }
    private readonly HttpClient _client = new();
    
    public Gotify(string endpoint, string appToken, Logger? logger = null) : base(NotificationManagerType.Gotify, logger)
    {
        this.endpoint = endpoint;
        this.appToken = appToken;
    }
    
    public override void SendNotification(string title, string notificationText)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{endpoint}/message");
        request.Headers.Add("X-Gotify-Key", this.appToken);
        request.Content = new StringContent(JsonConvert.SerializeObject(message, Formatting.None), Encoding.UTF8, "application/json");
        HttpResponseMessage response = _client.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            StreamReader sr = new (response.Content.ReadAsStream());
            logger?.WriteLine(this.GetType().ToString(), $"{response.StatusCode}: {sr.ReadToEnd()}");
        }
    }

    private class MessageData
    {
        public string message { get; }
        public long priority { get; }
        public string title { get; }
        public Dictionary<string, object> extras { get; }

        public MessageData(string title, string message)
        {
            this.title = title;
            this.message = message;
            this.extras = new();
            this.priority = 2;
        }
    }
}