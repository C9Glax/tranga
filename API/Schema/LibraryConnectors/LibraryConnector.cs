using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.LibraryConnectors;

[PrimaryKey("LibraryConnectorId")]
public abstract class LibraryConnector(string libraryConnectorId, LibraryType libraryType, string baseUrl, string auth) : APISerializable
{
    [MaxLength(64)]
    public string LibraryConnectorId { get; } = libraryConnectorId;

    public LibraryType LibraryType { get; init; } = libraryType;
    public string BaseUrl { get; init; } = baseUrl;
    public string Auth { get; init; } = auth;
}