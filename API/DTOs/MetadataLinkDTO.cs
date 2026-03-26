using Database.MangaContext;

namespace API.DTOs;

public sealed record MetadataLinkDTO
{
    public required Guid MetadataLinkId { get; init; }
    
    public required Guid MetadataExtensionId { get; init; }
    
    public Guid? CoverFileId { get; init; }
    
    public required Status Status { get; init; }
    
    public Rating? AgeRating { get; init; }
    
    public Demographic? Demographic { get; init; }
    
    public string? Url { get; init; }
    
    public string? Description { get; init; }
    
    public int? Year { get; init; }
    
    public string? Language { get; init; }
}