using System.Text;
using Newtonsoft.Json;

namespace Tranga.NotificationConnectors;

public class Gotify : NotificationConnector
{
    public string endpoint { get; }
    // ReSharper disable once MemberCanBePrivate.Global
    public string appToken { get; }
    private readonly HttpClient _client = new();
    
    [JsonConstructor]
    public Gotify(GlobalBase clone, string endpoint, string appToken) : base(clone, NotificationConnectorType.Gotify)
    {
        if (!baseUrlRex.IsMatch(endpoint))
            throw new ArgumentException("endpoint does not match pattern");
        this.endpoint = baseUrlRex.Match(endpoint).Value;;
        this.appToken = appToken;
    }

    public override string ToString()
    {
        return $"Gotify {endpoint}";
    }

    protected override void SendNotificationInternal(string title, string notificationText)
    {
        Log($"Sending notification: {title} - {notificationText}");
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{endpoint}/message");
        request.Headers.Add("X-Gotify-Key", this.appToken);
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