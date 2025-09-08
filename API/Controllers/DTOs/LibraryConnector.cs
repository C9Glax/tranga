using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Schema.LibraryContext.LibraryConnectors;

namespace API.Controllers.DTOs;

public record LibraryConnector(string Key, string BaseUrl, LibraryType Type) : Identifiable(Key)
{
    /// <summary>
    /// The Url of the Library instance
    /// </summary>
    [Required]
    [Url]
    [Description("The Url of the Library instance")]
    public string BaseUrl { get; init;} =  BaseUrl;

    /// <summary>
    /// The <see cref="LibraryType"/>
    /// </summary>
    [Required]
    [Description("The Library Type")]
    public LibraryType Type { get; init; } = Type;
}