using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreateNtfyConnectorRecord
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
    /// The Priority of Notifications
    /// </summary>
    [Required]
    [Description("The Priority of Notifications")]
    public required int Priority { get; init; }
    
    /// <summary>
    /// The Username used for authentication
    /// </summary>
    [Required]
    [Description("The Username used for authentication")]
    public required string Username { get; init; }
    
    /// <summary>
    /// The Password used for authentication
    /// </summary>
    [Required]
    [Description("The Password used for authentication")]
    public required string Password { get; init; }
    
    /// <summary>
    /// The Topic of Notifications
    /// </summary>
    [Required]
    [Description("The Topic of Notifications")]
    public required string Topic { get; init; }
}