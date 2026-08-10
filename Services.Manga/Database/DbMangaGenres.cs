namespace Services.Manga.Database;

/// <summary>
/// Join entity linking a <see cref="DbMetadata"/> entry to a <see cref="DbGenre"/>.
/// </summary>
public sealed record DbMangaGenres
{
    /// <summary>Foreign key to the associated <see cref="DbMetadata"/> entry.</summary>
    public required Guid MetadataId { get; init; }

    /// <summary>Foreign key to the associated <see cref="DbGenre"/>.</summary>
    public required string GenreId { get; init; }

    #region Navigations

    /// <summary>The metadata entry this genre is associated with.</summary>
    public DbMetadata? Metadata { get; internal set; }

    /// <summary>The genre associated with the metadata entry.</summary>
    public DbGenre? Genre { get; init; }

    #endregion
}
