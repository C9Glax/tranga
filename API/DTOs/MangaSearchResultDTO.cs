namespace API.DTOs;

/// <summary>
/// A Search Result
/// </summary>
public sealed record MangaSearchResultDTO
{
    /// <summary>
    /// The identifier of the Manga
    /// </summary>
    public required Guid MangaId { get; init; }
    
    /// <summary>
    /// The identifier of the Metadata entry
    /// </summary>
    public required Guid MetadataId { get; init; }
    
    /// <summary>
    /// The identifier of the Cover File
    /// </summary>
    public Guid? CoverFileId { get; init; }
    
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
    public string? Url { get; init; }
}