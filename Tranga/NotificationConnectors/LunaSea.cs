using System.Text;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class LunaSea(API.Schema.NotificationConnectors.Lunasea info) : NotificationConnector(info)
{
    public override void SendNotification(string title, string notificationText)
    {
        API.Schema.NotificationConnectors.Lunasea i = (API.Schema.NotificationConnectors.Lunasea)info;
        
        log.Info($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"https://notify.lunasea.app/v1/custom/{i.Id}");
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
        // ReSharper disable twice UnusedAutoPropertyAccessor.Local
        public string title { get; }
        public string body { get; }

        public MessageData(string title, string body)
        {
            this.title = title;
            this.body = body;
        }
    }
}