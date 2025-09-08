using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

public sealed record FileLibrary(string Key, string BasePath, string LibraryName) : Identifiable(Key)
{
    /// <summary>
    /// The directory Path of the library
    /// </summary>
    [Required]
    [Description("The directory Path of the library")]
    public string BasePath { get; internal set; } = BasePath;

    /// <summary>
    /// The Name of the library
    /// </summary>
    [Required]
    [Description("The Name of the library")]
    public string LibraryName { get; internal set; } = LibraryName;
}