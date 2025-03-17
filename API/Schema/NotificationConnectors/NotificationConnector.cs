using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.NotificationConnectors;

[PrimaryKey("Name")]
public class NotificationConnector(string name, string url, Dictionary<string, string> headers, string httpMethod, string body)
{
    [StringLength(64)]
    [Required]
    public string Name { get; init; } = name;
    
    [StringLength(2048)]
    [Required]
    [Url]
    public string Url { get; internal set; } = url;

    [Required]
    public Dictionary<string, string> Headers { get; internal set; } = headers;

    [StringLength(8)]
    [Required]
    public string HttpMethod { get; internal set; } = httpMethod;

    [StringLength(4096)]
    [Required]
    public string Body { get; internal set; } = body;

    [JsonIgnore]
    [NotMapped]
    private readonly HttpClient Client = new()
    {
        DefaultRequestHeaders = { { "User-Agent", TrangaSettings.userAgent } }
    };

    public void SendNotification(string title, string notificationText)
    {
        CustomWebhookFormatProvider formatProvider = new CustomWebhookFormatProvider(title, notificationText);
        string formattedUrl = string.Format(formatProvider, Url);
        string formattedBody = string.Format(formatProvider, Body, title, notificationText);
        Dictionary<string, string> formattedHeaders = Headers.ToDictionary(h => h.Key, 
            h => string.Format(formatProvider, h.Value, title, notificationText));

        HttpRequestMessage request = new(System.Net.Http.HttpMethod.Parse(HttpMethod), formattedUrl);
        foreach (var (key, value) in formattedHeaders)
            request.Headers.Add(key, value);
        request.Content = new StringContent(formattedBody);

        HttpResponseMessage response = Client.Send(request);
    }

    private class CustomWebhookFormatProvider(string title, string text) : IFormatProvider
    {
        public object? GetFormat(Type? formatType)
        {
            return this;
        }

        public string Format(string fmt, object arg, IFormatProvider provider)
        {
            if(arg.GetType() != typeof(string))
                return arg.ToString() ?? string.Empty;

            StringBuilder sb = new StringBuilder(fmt);
            sb.Replace("%title", title);
            sb.Replace("%text", text);
            
            return sb.ToString();
        }
    }
}