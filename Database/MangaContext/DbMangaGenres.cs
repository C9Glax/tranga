namespace Database.MangaContext;

public sealed record DbMangaGenres
{
    public required Guid MetadataSourceId { get; init; }
    
    public required string GenreId { get; init; }

    #region Navigations

    public DbMetadataSource? MetadataSource { get; internal set; }
    
    public DbGenre? Genre { get; init; }

    #endregion
}