using Database.MangaContext;

namespace API.DTOs;

/// <summary>
/// MetadataExtension Link Entry
/// </summary>
public sealed record MetadataLinkDTO
{
    /// <summary>
    /// ID of the MetadataExtension Link Entry
    /// </summary>
    public required Guid MetadataLinkId { get; init; }
    
    /// <summary>
    /// ID of the MetadataExtension
    /// </summary>
    public required Guid MetadataExtensionId { get; init; }
    
    /// <summary>
    /// ID of the Cover-File, if it exists
    /// </summary>
    public Guid? CoverFileId { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="Database.MangaContext.Status"/>
    /// </summary>
    public required Status Status { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="Database.MangaContext.Rating"/>
    /// </summary>
    public Rating? AgeRating { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="Database.MangaContext.Demographic"/>
    /// </summary>
    public Demographic? Demographic { get; init; }
    
    /// <summary>
    /// Url of the Metadata
    /// </summary>
    public string? Url { get; init; }
    
    /// <summary>
    /// Description of the Manga
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Release year of the Manga
    /// </summary>
    public int? Year { get; init; }
    
    /// <summary>
    /// Original Language of the Manga
    /// </summary>
    public string? Language { get; init; }
}