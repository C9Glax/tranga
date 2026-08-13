using Services.Libraries.Database;

namespace Services.Libraries.Entities;

/// <summary>
/// A configured library extension, as exposed to API consumers.
/// </summary>
public sealed record Library(LibraryServiceType LibraryServiceType, Guid Id, string Name, string BaseUrl)
{
    /// <summary>The kind of library extension this entry represents.</summary>
    public LibraryServiceType LibraryServiceType { get; init; } = LibraryServiceType;
    /// <summary>Unique identifier of this library connection.</summary>
    public Guid Id { get; init; } = Id;
    /// <summary>User-chosen display name for this library connection.</summary>
    public string Name { get; init; } = Name;
    /// <summary>Base URL of the library service.</summary>
    public string BaseUrl { get; init; } = BaseUrl;
}