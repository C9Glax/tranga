using Extensions.Data;

namespace Services.Libraries.Database;

/// <summary>
/// Persisted configuration for a library-extension connection (e.g. a Komga server) that Tranga
/// libraries and manga can be linked to.
/// </summary>
public sealed record DbLibraryService(LibraryServiceType LibraryServiceType, string Name, string BaseUrl, string ApiKey)
{
    /// <summary>Unique identifier of this library connection.</summary>
    public Guid LibraryServiceId { get; init; } = Guid.CreateVersion7();
    /// <summary>The kind of library extension this connection targets.</summary>
    public LibraryServiceType LibraryServiceType { get; init; } = LibraryServiceType;
    /// <summary>User-chosen display name for this library connection.</summary>
    public string Name { get; init; } = Name;
    /// <summary>Base URL of the library service (e.g. the Komga server's URL).</summary>
    public string BaseUrl { get; init; } = BaseUrl;
    /// <summary>API key used to authenticate against the library service.</summary>
    public string ApiKey { get; init; } = ApiKey;
    /// <summary>Username used when the API key was minted from credentials, if any.</summary>
    public string? Username { get; init; }
    /// <summary>Identifier of the library on the remote service (e.g. the Komga library ID) that this connection is bound to.</summary>
    public string TrangaLibraryId { get; internal set; }
}

/// <summary>The kind of external service a <see cref="DbLibraryService"/> connects to.</summary>
public enum LibraryServiceType
{
    /// <summary>A Komga server.</summary>
    Komga
}