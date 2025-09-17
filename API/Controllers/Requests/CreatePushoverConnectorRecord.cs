using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreatePushoverConnectorRecord
{
    /// <summary>
    /// The Name of the Notification Connector
    /// </summary>
    [Required]
    [Description("The Name of the Notification Connector")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The Apptoken used for authentication
    /// </summary>
    [Required]
    [Description("The Apptoken used for authentication")]
    public required string AppToken { get; init; }
    
    /// <summary>
    /// The Username used for authentication
    /// </summary>
    [Required]
    [Description("The Username used for authentication")]
    public required string Username { get; init; }
}