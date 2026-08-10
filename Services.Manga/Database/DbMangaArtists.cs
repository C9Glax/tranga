namespace Services.Manga.Database;

/// <summary>
/// Join entity linking a <see cref="DbMetadata"/> entry to a <see cref="DbPerson"/> credited as an artist.
/// </summary>
public sealed record DbMangaArtists
{
    /// <summary>Foreign key to the associated <see cref="DbMetadata"/> entry.</summary>
    public required Guid MetadataId { get; init; }

    /// <summary>Foreign key to the associated <see cref="DbPerson"/>.</summary>
    public required string ArtistId { get; init; }

    #region Navigations

    /// <summary>The metadata entry this artist is associated with.</summary>
    public DbMetadata? Metadata { get; internal set; }

    /// <summary>The person credited as an artist of the metadata entry.</summary>
    public DbPerson? Artist { get; init; }

    #endregion
}
