using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Entities;

/// <summary>
/// A <see cref="DownloadLink"/> as associated with a specific manga, including the match/priority/language
/// information relevant to that association (API/transport representation of <c>DbMangaDownloadLinks</c>).
/// </summary>
public sealed record MangaDownloadLink : DownloadLink
{
    /// <summary>The manga this download link is associated with.</summary>
    public required Guid MangaId { get; init; }

    /// <summary>Whether this download link has been confirmed as matching the associated manga.</summary>
    public required bool Matched { get; init; }

    /// <summary>The preference order of this download link relative to other links for the same manga (lower is preferred).</summary>
    public required int Priority { get; init; }

    /// <summary>The ISO language code of this download link's content, if known.</summary>
    [StringLength(8)]
    public string? Language { get; set; }
}
