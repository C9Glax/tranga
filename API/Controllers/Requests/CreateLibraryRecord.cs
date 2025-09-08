using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.Requests;

public sealed record CreateLibraryRecord(string BasePath, string LibraryName)
{
    /// <summary>
    /// The directory Path of the library
    /// </summary>
    [Required]
    [Description("The directory Path of the library")]
    public string BasePath { get; init; } = BasePath;
    
    /// <summary>
    /// The Name of the library
    /// </summary>
    [Required]
    [Description("The Name of the library")]
    public string LibraryName { get; init; } = LibraryName;
}