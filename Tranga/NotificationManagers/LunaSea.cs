using System.Text;
using Logging;
using Newtonsoft.Json;

namespace Tranga.NotificationManagers;

public class LunaSea : NotificationManager
{
    public string id { get; }
    private readonly HttpClient _client = new();
    public LunaSea(string id, Logger? logger = null) : base(NotificationManagerType.LunaSea, logger)
    {
        this.id = id;
    }

    public override void SendNotification(string title, string notificationText)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"https://notify.lunasea.app/v1/custom/{id}");
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
        public string title { get; }
        public string body { get; }
        public string image { get; }

        public MessageData(string title, string body)
        {
            this.title = title;
            this.body = body;
            this.image = "";
        }
    }
}