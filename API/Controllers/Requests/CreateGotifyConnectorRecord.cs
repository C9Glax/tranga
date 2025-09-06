using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreateGotifyConnectorRecord(string Name, string Url, string AppToken, int Priority)
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
    /// The Apptoken used for authentication
    /// </summary>
    [Required]
    [Description("The Apptoken used for authentication")]
    public string AppToken { get; init; } = AppToken;
    
    /// <summary>
    /// The Priority of Notifications
    /// </summary>
    [Required]
    [Description("The Priority of Notifications")]
    public int Priority { get; init; } = Priority;
}