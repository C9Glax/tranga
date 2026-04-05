namespace Database.MangaContext;

public sealed record DbMangaMetadataSource
{
    public Guid MangaId { get; init; }
    
    public Guid MetadataSourceId { get; init; }
    
    public required bool Chosen { get; set; }

    #region Navigations

    public required DbManga Manga { get; init; }
    
    public required DbMetadataSource MetadataSource { get; init; }

    #endregion
}