namespace Services.Manga.Database;

public sealed record DbMangaGenres
{
    public required Guid MetadataId { get; init; }
    
    public required string GenreId { get; init; }

    #region Navigations

    public DbMetadata? Metadata { get; internal set; }
    
    public DbGenre? Genre { get; init; }

    #endregion
}