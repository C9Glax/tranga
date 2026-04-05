namespace API.Entities;

public sealed record MetadataManga : MangaMetadata
{
    public required Guid[] MangaIds { get; init; }
}