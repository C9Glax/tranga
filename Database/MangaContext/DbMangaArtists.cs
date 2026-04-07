namespace Database.MangaContext;

public sealed record DbMangaArtists
{
    public required Guid MetadataId { get; init; }
    
    public required string ArtistId { get; init; }

    #region Navigations

    public DbMetadata? Metadata { get; internal set; }
    
    public DbPerson? Artist { get; init; }

    #endregion
}