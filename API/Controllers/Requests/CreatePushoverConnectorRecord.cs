using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreatePushoverConnectorRecord(string Name, string AppToken, string Username)
{
    /// <summary>
    /// The Name of the Notification Connector
    /// </summary>
    [Required]
    [Description("The Name of the Notification Connector")]
    public string Name { get; init; } = Name;
    
    /// <summary>
    /// The Apptoken used for authentication
    /// </summary>
    [Required]
    [Description("The Apptoken used for authentication")]
    public string AppToken { get; init; } = AppToken;
    
    /// <summary>
    /// The Username used for authentication
    /// </summary>
    [Required]
    [Description("The Username used for authentication")]
    public string Username { get; init; } = Username;
}