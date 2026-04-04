namespace Database.MangaContext;

public sealed record DbMangaAuthors
{
    public required Guid MetadataSourceId { get; init; }
    
    public required string AuthorId { get; init; }

    #region Navigations

    public DbMetadataSource? MetadataSource { get; internal set; }
    
    public DbPerson? Author { get; init; }

    #endregion
}