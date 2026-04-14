namespace Services.Manga.Database;

public sealed record DbMangaAuthors
{
    public required Guid MetadataId { get; init; }
    
    public required string AuthorId { get; init; }

    #region Navigations

    public DbMetadata? Metadata { get; internal set; }
    
    public DbPerson? Author { get; init; }

    #endregion
}