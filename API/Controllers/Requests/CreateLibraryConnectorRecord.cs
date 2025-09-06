using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Schema.LibraryContext.LibraryConnectors;

namespace API.Controllers.Requests;

public sealed record CreateLibraryConnectorRecord(LibraryType LibraryType, string Url, string Username, string Password)
{
    /// <summary>
    /// The <see cref="LibraryType"/>
    /// </summary>
    [Required]
    [Description("The Library Type")]
    public LibraryType LibraryType { get; init; } = LibraryType;
    
    /// <summary>
    /// The Url of the Library instance
    /// </summary>
    [Required]
    [Url]
    [Description("The Url of the Library instance")]
    public string Url { get; init; } = Url;
    
    /// <summary>
    /// The Username to authenticate to the Library instance
    /// </summary>
    [Required]
    [Description("The Username to authenticate to the Library instance")]
    public string Username { get; init; } = Username;
    
    /// <summary>
    /// The Password to authenticate to the Library instance
    /// </summary>
    [Required]
    [Description("The Password to authenticate to the Library instance")]
    public string Password { get; init; } = Password;
}