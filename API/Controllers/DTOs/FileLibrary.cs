using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

public sealed record FileLibrary(string Key, string BasePath, string LibraryName) : Identifiable(Key)
{
    [StringLength(256)]
    [Required]
    public string BasePath { get; internal set; } = BasePath;

    [StringLength(512)]
    [Required]
    public string LibraryName { get; internal set; } = LibraryName;
}