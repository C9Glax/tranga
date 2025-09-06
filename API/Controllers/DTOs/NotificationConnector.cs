using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

public record NotificationConnector(string Name, string Url, string HttpMethod, string Body, Dictionary<string, string> Headers) : Identifiable(Name)
{
    /// <summary>
    /// The Name of the Notification Connector
    /// </summary>
    [Required]
    [Description("The Name of the Notification Connector")]
    public string Name { get; init; } = Name;
    
    /// <summary>
    /// The Url of the Instance
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Url]
    [Description("The Url of the Instance")]
    public string Url { get; internal set; } = Url;
    
    /// <summary>
    /// The HTTP Request Method to use for notifications
    /// </summary>
    [Required]
    [Description("The HTTP Request Method to use for notifications")]
    public string HttpMethod { get; internal set; } = HttpMethod;

    /// <summary>
    /// The Request Body to use to send notifications
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Description("The Request Body to use to send notifications")]
    public string Body { get; internal set; } = Body;

    /// <summary>
    /// The Request Headers to use to send notifications
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Description("The Request Headers to use to send notifications")]
    public Dictionary<string, string> Headers { get; internal set; } = Headers;
}