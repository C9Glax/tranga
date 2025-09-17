using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public sealed record CreateLibraryRecord
{
    /// <summary>
    /// The directory Path of the library
    /// </summary>
    [Required]
    [Description("The directory Path of the library")]
    public required string BasePath { get; init; }
    
    /// <summary>
    /// The Name of the library
    /// </summary>
    [Required]
    [Description("The Name of the library")]
    public required string LibraryName { get; init; }
}