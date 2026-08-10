using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Entities;

/// <summary>
/// API-facing representation of a manga's candidate download source link, as retrieved from a download extension.
/// </summary>
public record DownloadLink
{
    /// <summary>The unique identifier of this download link.</summary>
    public required Guid DownloadId { get; init; }

    /// <summary>Identifier of the download extension this link was retrieved from.</summary>
    public required Guid DownloadExtensionId { get; init; }

    /// <summary>The identifier of the series as known to the source download extension.</summary>
    public required string Identifier { get; init; }

    /// <summary>The series title.</summary>
    [StringLength(1024)]
    public required string Series { get; set; }

    /// <summary>A synopsis/description of the series.</summary>
    [StringLength(4096)]
    public required string? Summary { get; set; }

    /// <summary>The language code of the series content.</summary>
    [StringLength(8)]
    public string? Language { get; set; }

    /// <summary>The URL of the series on the source site.</summary>
    public required string? Url { get; init; }

    /// <summary>The identifier of the cover image file, if one has been downloaded.</summary>
    public required Guid? CoverId { get; set; }

    /// <summary>Whether the series is flagged as not safe for work.</summary>
    public required bool? NSFW { get; init; }
}