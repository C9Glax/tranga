using System.Text;
using Newtonsoft.Json;

namespace API.Schema.NotificationConnectors;

public class Lunasea(string id)
    : NotificationConnector(TokenGen.CreateToken(typeof(Lunasea), id), NotificationConnectorType.LunaSea)
{
    public string Id { get; init; } = id;
    public override void SendNotification(string title, string notificationText)
    {
        MessageData message = new(title, notificationText);
        HttpRequestMessage request = new(HttpMethod.Post, $"https://notify.lunasea.app/v1/custom/{id}");
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