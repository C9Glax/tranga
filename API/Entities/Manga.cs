namespace API.Entities;

public sealed record Manga
{
    public required Guid MangaId { get; init; }
    
    public required bool Monitored { get; init; }
    
    public Metadata? MetadataEntry { get; init; }
}