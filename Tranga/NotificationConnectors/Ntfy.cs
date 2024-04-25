using System.Text;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class Ntfy : NotificationConnector
{
    // ReSharper disable twice MemberCanBePrivate.Global
    public string endpoint { get; init; }
    public string auth { get; init; }
    private const string Topic = "tranga";
    private readonly HttpClient _client = new();
    
    [JsonConstructor]
    public Ntfy(GlobalBase clone, string endpoint, string auth) : base(clone, NotificationConnectorType.Ntfy)
    {
        if (!baseUrlRex.IsMatch(endpoint))
            throw new ArgumentException("endpoint does not match pattern");
        this.endpoint = endpoint;
        this.auth = auth;
    }

    public override string ToString()
    {
        return $"Ntfy {endpoint} {Topic}";
    }

    public override void SendNotification(string title, string notificationText)
    {
        Log($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{this.endpoint}?auth={this.auth}");
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
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string topic { get; }
        public string title { get; }
        public string message { get; }
        public int priority { get; }

        public MessageData(string title, string message)
        {
            this.topic = Topic;
            this.title = title;
            this.message = message;
            this.priority = 3;
        }
    }
}