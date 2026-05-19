namespace Services.Libraries.Entities;

public sealed record ServiceDirectoryMapping(Guid MappingId, Guid LibraryId, string TrangaPath, string ServicePath)
{
    public Guid MappingId { get; init; } = MappingId;

    public Guid LibraryId { get; init; } = LibraryId;

    /// <summary>
    /// The path in the Tranga-Service
    /// </summary>
    public string TrangaPath { get; set; } = TrangaPath;

    /// <summary>
    /// The path that points to the same location in the service
    /// </summary>
    public string ServicePath { get; set; } = ServicePath;
}