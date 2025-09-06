using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public record CreateNtfyConnectorRecord(string Name, string Url, string Username, string Password, string Topic, int Priority)
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
    /// The Priority of Notifications
    /// </summary>
    [Required]
    [Description("The Priority of Notifications")]
    public int Priority { get; init; } = Priority;
    
    /// <summary>
    /// The Username used for authentication
    /// </summary>
    [Required]
    [Description("The Username used for authentication")]
    public string Username { get; init; } = Username;
    
    /// <summary>
    /// The Password used for authentication
    /// </summary>
    [Required]
    [Description("The Password used for authentication")]
    public string Password { get; init; } = Password;
    
    /// <summary>
    /// The Topic of Notifications
    /// </summary>
    [Required]
    [Description("The Topic of Notifications")]
    public string Topic { get; init; } = Topic;
}