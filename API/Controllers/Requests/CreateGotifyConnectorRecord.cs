using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreateGotifyConnectorRecord
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
    /// The Apptoken used for authentication
    /// </summary>
    [Required]
    [Description("The Apptoken used for authentication")]
    public required string AppToken { get; init; }
    
    /// <summary>
    /// The Priority of Notifications
    /// </summary>
    [Required]
    [Description("The Priority of Notifications")]
    public required int Priority { get; init; }
}