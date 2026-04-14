namespace Services.Manga.Database;

public sealed record DbMangaDownloadLinks
{
    public Guid MangaId { get; init; }
    
    public Guid DownloadLinkId { get; init; }
    
    public required bool Matched { get; set; }
    
    public required int Priority { get; set; }

    #region Navigations

    public required DbManga Manga { get; init; }
    
    public required DbDownloadLink DownloadLink { get; init; }

    #endregion
}