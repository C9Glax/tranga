namespace Database.MangaContext;

public sealed record DbChapterDownloadSource
{
    public required Guid ChapterId { get; init; }
    
    public required Guid DownloadExtension { get; init; }
    
    public required string Identifier { get; init; }
    
    public required int Priority { get; set; }
    
    public Guid? FileId { get; set; }
    
    public string? Url { get; set; }

    #region Navigations

    public DbChapter? Chapter { get; internal set; }
    
    public DbFile? File { get; internal set; }

    #endregion
}