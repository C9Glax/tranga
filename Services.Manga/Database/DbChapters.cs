using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Database;

/// <summary>
/// Database entity representing a single chapter of a manga.
/// </summary>
public sealed record DbChapter
{
    /// <summary>The unique identifier of the chapter.</summary>
    public Guid ChapterId { get; internal set; }

    /// <summary>Foreign key to the owning <see cref="DbManga"/>.</summary>
    public required Guid MangaId { get; init; }

    /// <summary>The chapter's title, if any.</summary>
    [StringLength(2048)]
    public string? Title { get; set; }

    /// <summary>The volume the chapter belongs to, if the series is organized into volumes.</summary>
    [StringLength(16)]
    public string? Volume { get; set; }

    /// <summary>The chapter number, as a string to allow for values like "10.5".</summary>
    [StringLength(16)]
    public required string Number { get; set; }

    /// <summary>The date the chapter was released, if known.</summary>
    public DateTimeOffset? ReleaseDate { get; set; }

    #region Navigations

    /// <summary>The manga this chapter belongs to.</summary>
    public DbManga? Manga { get; internal set; }

    /// <summary>The per-extension download links for this chapter.</summary>
    public ICollection<DbChapterDownloadLink>? DownloadLinks { get; internal set; }

    #endregion
}