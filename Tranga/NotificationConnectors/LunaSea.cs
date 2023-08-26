using System.Text;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class LunaSea : NotificationConnector
{
    // ReSharper disable once MemberCanBePrivate.Global
    public string id { get; init; }
    private readonly HttpClient _client = new();
    
    [JsonConstructor]
    public LunaSea(GlobalBase clone, string id) : base(clone, NotificationManagerType.LunaSea)
    {
        this.id = id;
    }

    public override void SendNotification(string title, string notificationText)
    {
        Log($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"https://notify.lunasea.app/v1/custom/{id}");
        request.Content = new StringContent(JsonConvert.SerializeObject(message, Formatting.None), Encoding.UTF8, "application/json");
        HttpResponseMessage response = _client.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            StreamReader sr = new (response.Content.ReadAsStream());
            Log($"{response.StatusCode}: {sr.ReadToEnd()}");
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