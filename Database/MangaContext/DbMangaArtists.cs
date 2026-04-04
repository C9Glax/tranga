namespace Database.MangaContext;

public sealed record DbMangaArtists
{
    public required Guid MetadataSourceId { get; init; }
    
    public required string ArtistId { get; init; }

    #region Navigations

    public DbMetadataSource? MetadataSource { get; internal set; }
    
    public DbPerson? Artist { get; init; }

    #endregion
}