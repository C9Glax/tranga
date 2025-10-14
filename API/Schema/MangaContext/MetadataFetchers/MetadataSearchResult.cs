using System.ComponentModel.DataAnnotations;

namespace API.Schema.MangaContext.MetadataFetchers;

public record MetadataSearchResult(
    string Identifier,
    string Name,
    string Url,
    string? Description = null,
    string? CoverUrl = null)
{
    /// <summary>
    /// Identifier specific to the MetadataFetcher
    /// </summary>
    [Required]
    public string Identifier { get; init; } = Identifier;
    /// <summary>
    /// Name of the Manga
    /// </summary>
    [Required]
    public string Name { get; init; } = Name;
    /// <summary>
    /// Url to the result
    /// </summary>
    [Required]
    public string Url { get; init; } = Url;
    /// <summary>
    /// Description of the Manga
    /// </summary>
    [Required]
    public string? Description { get; init; } = Description;
    /// <summary>
    /// Url to the cover if available
    /// </summary>
    [Required]
    public string? CoverUrl { get; init; } = CoverUrl;
}