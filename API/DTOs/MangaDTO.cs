namespace API.DTOs;

/// <summary>
/// Manga Info
/// </summary>
public sealed record MangaDTO
{
    /// <summary>
    /// ID of the Manga
    /// </summary>
    public required Guid MangaId { get; init; }
    
    /// <summary>
    /// Title of the Manga
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Monitoring status of the Manga
    /// </summary>
    public required bool Monitored { get; init; }
    
    /// <summary>
    /// Linked MetadataExtension Entries
    /// </summary>
    public MetadataLinkDTO[]? MetadataLinks { get; init; } = null;

    /// <summary>
    /// Linked DownloadExtension Entries
    /// </summary>
    public DownloadLinkDTO[]? DownloadLinks { get; init; } = null;

}