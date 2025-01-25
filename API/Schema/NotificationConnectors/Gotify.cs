using System.Text;
using Newtonsoft.Json;

namespace API.Schema.NotificationConnectors;

public class Gotify(string endpoint, string appToken)
    : NotificationConnector(TokenGen.CreateToken(typeof(Gotify), endpoint), NotificationConnectorType.Gotify)
{
    public string Endpoint { get; init; } = endpoint;
    public string AppToken { get; init; } = appToken;
    
    public override void SendNotification(string title, string notificationText)
    {
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"{endpoint}/message");
        request.Headers.Add("X-Gotify-Key", this.AppToken);
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