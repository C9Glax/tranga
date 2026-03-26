namespace API.DTOs;

/// <summary>
/// A Search Result
/// </summary>
public sealed record MangaMatchResultDTO
{
    /// <summary>
    /// The identifier of the Manga
    /// </summary>
    public required Guid MangaId { get; init; }
    
    /// <summary>
    /// The identifier of the Download entry
    /// </summary>
    public required Guid DownloadId { get; init; }
    
    /// <summary>
    /// The title of the Manga
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// The description of the Manga
    /// </summary>
    public required string? Description { get; init; }
    
    /// <summary>
    /// Url of the Manga
    /// </summary>
    public required string Url { get; init; }
}