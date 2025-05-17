using System.ComponentModel.DataAnnotations;

namespace API.Schema;

public class LocalLibrary(string basePath, string libraryName)
{
    [StringLength(64)] 
    [Required]
    public string LocalLibraryId { get; init; } = TokenGen.CreateToken(typeof(LocalLibrary), basePath);
    [StringLength(256)]
    [Required]
    public string BasePath { get; internal set; } = basePath;

    [StringLength(512)]
    [Required]
    public string LibraryName { get; internal set; } = libraryName;

    public override string ToString()
    {
        return $"{LocalLibraryId} {LibraryName} - {BasePath}";
    }
}