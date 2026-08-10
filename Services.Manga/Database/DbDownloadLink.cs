using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a candidate download source (series match) for a manga, as retrieved from a
/// download extension (e.g. MangaDex, WeebCentral). A manga can have several of these; the ones linked via a
/// "matched" <see cref="DbMangaDownloadLinks"/> are actively used to fetch chapters, in <see cref="DbMangaDownloadLinks.Priority"/> order.
/// </summary>
public sealed record DbDownloadLink
{
    /// <summary>The unique identifier of this download link.</summary>
    public Guid DownloadLinkId { get; internal set; }

    /// <summary>Identifier of the download extension (see <c>IDownloadExtension.DownloadExtensionsId</c>) this link was retrieved from.</summary>
    public required Guid DownloadExtension { get; init; }

    /// <summary>The identifier of the series as known to the source download extension.</summary>
    public required string Identifier { get; init; }

    /// <summary>The series title.</summary>
    [StringLength(1024)]
    public required string Series { get; set; }

    /// <summary>A synopsis/description of the series.</summary>
    [StringLength(4096)]
    public string? Summary { get; set; }

    /// <summary>The language code of the series content.</summary>
    [StringLength(8)]
    public string? Language { get; set; }

    /// <summary>The URL of the series on the source site.</summary>
    public string? Url { get; set; }

    /// <summary>Foreign key to the <see cref="DbFile"/> holding the cover image.</summary>
    public Guid? CoverId { get; set; }

    /// <summary>Whether the series is flagged as not safe for work.</summary>
    public bool? NSFW { get; set; }

    #region Navigations

    /// <summary>The manga this download link is a candidate match for.</summary>
    public ICollection<DbMangaDownloadLinks>? MangaMatches { get; init; }

    /// <summary>The cover image file for this download link.</summary>
    public DbFile? Cover { get; set; }

    #endregion
}