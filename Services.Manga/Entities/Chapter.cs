using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Entities;

/// <summary>
/// API-facing representation of a single chapter of a manga.
/// </summary>
public record Chapter
{
    /// <summary>The unique identifier of the chapter.</summary>
    public required Guid ChapterId { get; init; }

    /// <summary>The identifier of the manga this chapter belongs to.</summary>
    public required Guid MangaId { get; init; }

    /// <summary>The chapter's title, if any.</summary>
    [StringLength(2048)]
    public required string? Title { get; set; }

    /// <summary>The volume the chapter belongs to, if the series is organized into volumes.</summary>
    [StringLength(16)]
    public required string? Volume { get; set; }

    /// <summary>The chapter number, as a string to allow for values like "10.5".</summary>
    [StringLength(16)]
    public required string Number { get; set; }

    /// <summary>The date the chapter was released, if known.</summary>
    public required DateTimeOffset? ReleaseDate { get; set; }
}