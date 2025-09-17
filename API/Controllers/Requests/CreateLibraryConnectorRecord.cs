using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Schema.LibraryContext.LibraryConnectors;

namespace API.Controllers.Requests;

public sealed record CreateLibraryConnectorRecord
{
    /// <summary>
    /// The <see cref="LibraryType"/>
    /// </summary>
    [Required]
    [Description("The Library Type")]
    public required LibraryType LibraryType { get; init; }
    
    /// <summary>
    /// The Url of the Library instance
    /// </summary>
    [Required]
    [Url]
    [Description("The Url of the Library instance")]
    public required string Url { get; init; }
    
    /// <summary>
    /// The Username to authenticate to the Library instance
    /// </summary>
    [Required]
    [Description("The Username to authenticate to the Library instance")]
    public required string Username { get; init; }
    
    /// <summary>
    /// The Password to authenticate to the Library instance
    /// </summary>
    [Required]
    [Description("The Password to authenticate to the Library instance")]
    public required  string Password { get; init; }
}