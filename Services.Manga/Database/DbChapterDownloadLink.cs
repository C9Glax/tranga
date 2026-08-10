namespace Services.Manga.Database;

/// <summary>
/// Join entity linking a <see cref="DbChapter"/> to a specific download extension, tracking the chapter's
/// identifier on that source, its download priority relative to other extensions, and the downloaded file once fetched.
/// </summary>
public sealed record DbChapterDownloadLink
{
    /// <summary>Foreign key to the associated <see cref="DbChapter"/>.</summary>
    public required Guid ChapterId { get; init; }

    /// <summary>Identifier of the download extension (see <c>IDownloadExtension.DownloadExtensionsId</c>) this link refers to.</summary>
    public required Guid DownloadExtension { get; init; }

    /// <summary>The identifier of the chapter as known to the source download extension.</summary>
    public required string Identifier { get; init; }

    /// <summary>The download priority of this extension relative to other extensions for the same chapter; lower values are preferred.</summary>
    public required int Priority { get; set; }

    /// <summary>Foreign key to the downloaded <see cref="DbFile"/>, once the chapter has been fetched from this source.</summary>
    public Guid? FileId { get; set; }

    /// <summary>The URL of the chapter on the source site.</summary>
    public string? Url { get; set; }

    #region Navigations

    /// <summary>The chapter this link belongs to.</summary>
    public DbChapter? Chapter { get; set; }

    /// <summary>The downloaded chapter file, once fetched from this source.</summary>
    public DbFile? File { get; internal set; }

    #endregion
}