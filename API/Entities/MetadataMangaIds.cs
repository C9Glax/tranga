namespace API.Entities;

public sealed record MetadataMangaIds : Metadata
{
    public required Guid[] MangaIds { get; init; }
}