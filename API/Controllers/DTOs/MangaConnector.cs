using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

public sealed record MangaConnector(string Name, bool Enabled, string IconUrl, string[] SupportedLanguages) : Identifiable(Name)
{
    /// <summary>
    /// Whether Connector is used for Searches and Downloads
    /// </summary>
    [Required]
    [Description("Whether Connector is used for Searches and Downloads")]
    public bool Enabled { get; init; } = Enabled;
    
    /// <summary>
    /// Languages supported by the Connector
    /// </summary>
    [Required]
    [Description("Languages supported by the Connector")]
    public string[] SupportedLanguages { get; init; } = SupportedLanguages;
    
    /// <summary>
    /// Url of the Website Icon
    /// </summary>
    [Required]
    [Description("Url of the Website Icon")]
    public string IconUrl { get; init; } = IconUrl;
}