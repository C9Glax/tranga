namespace Database.MangaContext;

public sealed record DbMangaMetadataEntries
{
    public Guid MangaId { get; init; }
    
    public Guid MetadataId { get; init; }
    
    public required bool Chosen { get; set; }

    #region Navigations

    public required DbManga Manga { get; init; }
    
    public required DbMetadata Metadata { get; init; }

    #endregion
}