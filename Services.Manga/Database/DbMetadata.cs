using System.ComponentModel.DataAnnotations;
using Common.Datatypes;

namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a candidate metadata record for a manga, as retrieved from a metadata extension
/// (e.g. MangaDex, MangaUpdates). A manga can have several of these; the one linked via a "chosen"
/// <see cref="DbMangaMetadataEntries"/> is treated as authoritative.
/// </summary>
public sealed record DbMetadata
{
    /// <summary>The unique identifier of this metadata entry.</summary>
    public Guid MetadataId { get; internal set; }

    /// <summary>Identifier of the metadata extension (see <c>IMetadataExtension.MetadataExtensionId</c>) this entry was retrieved from.</summary>
    public required Guid MetadataExtension { get; init; }

    /// <summary>The identifier of the series as known to the source metadata extension.</summary>
    public required string Identifier { get; init; }

    /// <summary>The series title.</summary>
    [StringLength(1024)]
    public required string Series { get; set; }

    /// <summary>A synopsis/description of the series.</summary>
    [StringLength(4096)]
    public string? Summary { get; set; }

    /// <summary>The year the series was first published.</summary>
    public int? Year { get; set; }

    /// <summary>The language code of the series content.</summary>
    [StringLength(8)]
    public string? Language { get; set; }

    /// <summary>The total number of chapters in the series, if known.</summary>
    public int? ChaptersNumber { get; set; }

    /// <summary>The publication status of the series (e.g. ongoing, completed).</summary>
    public ReleaseStatus? Status { get; set; }

    /// <summary>Foreign key to the <see cref="DbFile"/> holding the cover image.</summary>
    public Guid? CoverId { get; set; }

    /// <summary>The URL of the series on the source site.</summary>
    public string? Url { get; set; }

    /// <summary>Whether the series is flagged as not safe for work.</summary>
    public bool? NSFW { get; set; }

    #region Navigations

    /// <summary>The cover image file for this metadata entry.</summary>
    public DbFile? Cover { get; set; }

    /// <summary>The genres associated with this metadata entry.</summary>
    public ICollection<DbGenre>? Genres { get; set; }

    /// <summary>The artists associated with this metadata entry.</summary>
    public ICollection<DbPerson>? Artists { get; set; }

    /// <summary>The authors associated with this metadata entry.</summary>
    public ICollection<DbPerson>? Authors { get; set; }

    /// <summary>The manga(s) this metadata entry is a candidate match for.</summary>
    public ICollection<DbMangaMetadataEntries>? MangaMetadataEntries { get; init; }

    #endregion
}