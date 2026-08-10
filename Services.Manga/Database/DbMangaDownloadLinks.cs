namespace Services.Manga.Database;

/// <summary>
/// Join entity linking a <see cref="DbManga"/> to a candidate <see cref="DbDownloadLink"/> sourced from a download extension.
/// </summary>
public sealed record DbMangaDownloadLinks
{
    /// <summary>Foreign key to the associated <see cref="DbManga"/>.</summary>
    public Guid MangaId { get; init; }

    /// <summary>Foreign key to the associated <see cref="DbDownloadLink"/>.</summary>
    public Guid DownloadLinkId { get; init; }

    /// <summary>Whether this download link is confirmed as a correct match and actively used to fetch chapters.</summary>
    public required bool Matched { get; set; }

    /// <summary>The download priority of this source relative to other matched sources for the same manga; lower values are preferred.</summary>
    public required int Priority { get; set; }

    #region Navigations

    /// <summary>The manga this download link is associated with.</summary>
    public required DbManga Manga { get; init; }

    /// <summary>The download link associated with the manga.</summary>
    public required DbDownloadLink DownloadLink { get; init; }

    #endregion
}