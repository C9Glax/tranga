namespace Database.MangaContext;

public sealed record DbMangaDownloadSource
{
    public Guid MangaId { get; init; }
    
    public Guid DownloadSourceId { get; init; }
    
    public required bool Matched { get; set; }
    
    public required int Priority { get; set; }

    #region Navigations

    public required DbManga Manga { get; init; }
    
    public required DbDownloadSource DownloadSource { get; init; }

    #endregion
}