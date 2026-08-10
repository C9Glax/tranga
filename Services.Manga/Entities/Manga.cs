namespace Services.Manga.Entities;

/// <summary>
/// API/transport representation of a manga series, including its currently chosen metadata and known download links.
/// </summary>
public sealed record Manga
{
    /// <summary>The unique identifier of the manga.</summary>
    public required Guid MangaId { get; init; }

    /// <summary>Whether this manga is being actively monitored for new chapters/updates.</summary>
    public required bool Monitored { get; init; }

    /// <summary>The currently chosen metadata entry for this manga, if any.</summary>
    public Metadata? MetadataEntry { get; init; }

    /// <summary>The download links associated with this manga, if any.</summary>
    public DownloadLink[]? DownloadLinks { get; init; }
}
