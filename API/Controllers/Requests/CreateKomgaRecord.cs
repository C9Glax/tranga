using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Schema.LibraryContext.LibraryConnectors;

namespace API.Controllers.Requests;

public sealed record CreateKomgaRecord
{
    
    /// <summary>
    /// The Url of the Library instance
    /// </summary>
    [Required]
    [Url]
    [Description("The Url of the Library instance")]
    public required string Url { get; init; }
    
    /// <summary>
    /// The ApiKey to authenticate to the Library instance
    /// </summary>
    [Required]
    [Description("The ApiKey to authenticate to the Library instance")]
    public required string ApiKey { get; init; }
}