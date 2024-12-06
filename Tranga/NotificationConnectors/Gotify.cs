using System.Text;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class Gotify(API.Schema.NotificationConnectors.Gotify info) : NotificationConnector(info)
{
    public override void SendNotification(string title, string notificationText)
    {
        API.Schema.NotificationConnectors.Gotify i = (API.Schema.NotificationConnectors.Gotify)info;
        
        log.Info($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{i.Endpoint}/message");
        request.Headers.Add("X-Gotify-Key", i.AppToken);
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
        // ReSharper disable four times UnusedAutoPropertyAccessor.Local
        public string message { get; }
        public long priority { get; }
        public string title { get; }
        public Dictionary<string, object> extras { get; }

        public MessageData(string title, string message)
        {
            this.title = title;
            this.message = message;
            this.extras = new();
            this.priority = 4;
        }
    }
}