namespace API.DTOs;

/// <summary>
/// The result of <see cref="Features.PostSearchMangaEndpoint"/>
/// </summary>
public sealed record MangaSearchResult
{
    /// <summary>
    /// The title of the Manga
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// The description of the Manga
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// The cover image as Base64 string
    /// </summary>
    public required string CoverBase64 { get; init; }
}