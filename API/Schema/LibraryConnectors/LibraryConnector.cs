using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.LibraryConnectors;

[PrimaryKey("LibraryConnectorId")]
public abstract class LibraryConnector(string libraryConnectorId, LibraryType libraryType, string baseUrl, string auth)
{
    [StringLength(64)]
    [Required]
    public string LibraryConnectorId { get; } = libraryConnectorId;

    [Required]
    public LibraryType LibraryType { get; init; } = libraryType;
    [StringLength(256)]
    [Required]
    [Url]
    public string BaseUrl { get; init; } = baseUrl;
    [StringLength(256)]
    [Required]
    public string Auth { get; init; } = auth;
    
    protected abstract void UpdateLibraryInternal();
    internal abstract bool Test();
}