using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreateNotificationConnectorRecord
{
    /// <summary>
    /// The Name of the Notification Connector
    /// </summary>
    [Required]
    [Description("The Name of the Notification Connector")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The Url of the Instance
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Url]
    [Description("The Url of the Instance")]
    public required string Url { get; init; }
    
    /// <summary>
    /// The HTTP Request Method to use for notifications
    /// </summary>
    [Required]
    [Description("The HTTP Request Method to use for notifications")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// The Request Body to use to send notifications
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Description("The Request Body to use to send notifications")]
    public required string Body { get; init; }

    /// <summary>
    /// The Request Headers to use to send notifications
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" will be replaced when notifications are sent</remarks>
    [Required]
    [Description("The Request Headers to use to send notifications")]
    public required Dictionary<string, string> Headers { get; init; }
}