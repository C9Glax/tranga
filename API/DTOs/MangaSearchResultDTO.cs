namespace API.DTOs;

/// <summary>
/// A Search Result
/// </summary>
public sealed record MangaSearchResultDTO
{
    /// <summary>
    /// The title of the Manga
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// The description of the Manga
    /// </summary>
    public required string? Description { get; init; }
    
    /// <summary>
    /// The cover image as Base64 string
    /// </summary>
    public required string CoverBase64 { get; init; }
    
    /// <summary>
    /// Url of the Manga
    /// </summary>
    public required string Url { get; init; }
}