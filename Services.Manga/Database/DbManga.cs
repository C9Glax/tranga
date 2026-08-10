namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a manga series, independent of any particular metadata or download source.
/// </summary>
public sealed record DbManga
{
    /// <summary>The unique identifier of the manga.</summary>
    public Guid MangaId { get; init; }

    /// <summary>Whether this manga is being actively monitored for new chapters/updates.</summary>
    public required bool Monitored { get; set; } = false;

    #region Navigations

    /// <summary>The chapters belonging to this manga.</summary>
    public ICollection<DbChapter>? Chapters { get; init; }

    /// <summary>The candidate metadata entries (from various metadata extensions) associated with this manga.</summary>
    public ICollection<DbMangaMetadataEntries>? MetadataEntries { get; init; }

    /// <summary>The download-source links (from various download extensions) associated with this manga.</summary>
    public ICollection<DbMangaDownloadLinks>? DownloadLinks { get; init; }

    #endregion
}
