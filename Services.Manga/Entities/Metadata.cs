using System.ComponentModel.DataAnnotations;
using Common.Datatypes;

namespace Services.Manga.Entities;

/// <summary>
/// API-facing representation of a manga's candidate metadata entry, as retrieved from a metadata extension.
/// </summary>
public record Metadata
{
    /// <summary>The unique identifier of this metadata entry.</summary>
    public required Guid MetadataId { get; init; }

    /// <summary>Identifier of the metadata extension this entry was retrieved from.</summary>
    public required Guid MetadataExtensionId { get; init; }

    /// <summary>The identifier of the series as known to the source metadata extension.</summary>
    public required string Identifier { get; init; }

    /// <summary>Whether this metadata entry is the one currently selected as authoritative for the manga.</summary>
    public bool? Chosen { get; init; }

    /// <summary>The series title.</summary>
    [StringLength(1024)]
    public required string Series { get; set; }

    /// <summary>A synopsis/description of the series.</summary>
    [StringLength(4096)]
    public required string? Summary { get; set; }

    /// <summary>The year the series was first published.</summary>
    public int? Year { get; set; }

    /// <summary>The language code of the series content.</summary>
    [StringLength(8)]
    public string? Language { get; set; }

    /// <summary>The total number of chapters in the series, if known.</summary>
    public int? ChaptersNumber { get; set; }

    /// <summary>The identifier of the cover image file, if one has been downloaded.</summary>
    public required Guid? CoverId { get; set; }

    /// <summary>The genres associated with the series.</summary>
    public string[] Genres { get; init; }

    /// <summary>The authors of the series.</summary>
    public string[] Authors { get; init; }

    /// <summary>The artists of the series.</summary>
    public string[] Artists { get; init; }

    /// <summary>The URL of the series on the source site.</summary>
    public required string? Url { get; init; }

    /// <summary>The publication status of the series (e.g. ongoing, completed).</summary>
    public ReleaseStatus? Status { get; init; }

    /// <summary>Whether the series is flagged as not safe for work.</summary>
    public required bool? NSFW { get; init; }
}