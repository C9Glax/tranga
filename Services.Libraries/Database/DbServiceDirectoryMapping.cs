namespace Services.Libraries.Database;

/// <summary>
/// Represents a mapping:
/// If file xyz is located at <i>/Manga/xyz</i> in the Tranga service container,
/// and at <i>/import/Manga/xyz</i> in the foreign service container the mapping should be <br />
/// <see cref="TrangaPath"/> = <i>/Manga</i> <br />
/// <see cref="ServicePath"/> = <i>/import/Manga</i>
/// </summary>
public sealed record DbServiceDirectoryMapping(Guid LibraryId, string TrangaPath, string ServicePath)
{
    public Guid MappingId { get; init; } = Guid.CreateVersion7();

    public Guid LibraryId { get; init; } = LibraryId;

    /// <summary>
    /// The path in the Tranga-Service
    /// </summary>
    public string TrangaPath { get; set; } = TrangaPath;

    /// <summary>
    /// The path that points to the same location in the service
    /// </summary>
    public string ServicePath { get; set; } = ServicePath;

    #region Navigations

    internal DbLibrary? Library { get; init; }

    #endregion
}