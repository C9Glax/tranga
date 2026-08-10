namespace Services.Manga.Database;

/// <summary>
/// Join entity linking a <see cref="DbManga"/> to a candidate <see cref="DbMetadata"/> entry sourced from a metadata extension.
/// </summary>
public sealed record DbMangaMetadataEntries
{
    /// <summary>Foreign key to the associated <see cref="DbManga"/>.</summary>
    public Guid MangaId { get; init; }

    /// <summary>Foreign key to the associated <see cref="DbMetadata"/> entry.</summary>
    public Guid MetadataId { get; init; }

    /// <summary>Whether this metadata entry is the one currently selected as authoritative for the manga.</summary>
    public required bool Chosen { get; set; }

    #region Navigations

    /// <summary>The manga this metadata entry belongs to.</summary>
    public required DbManga Manga { get; init; }

    /// <summary>The metadata entry associated with the manga.</summary>
    public required DbMetadata Metadata { get; init; }

    #endregion
}
