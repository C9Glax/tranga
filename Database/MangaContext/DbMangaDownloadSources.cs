namespace Database.MangaContext;

public sealed record DbMangaDownloadSources
{
    public required Guid MangaId { get; init; }
    
    public required Guid DownloadExtension { get; init; }
    
    public required string Identifier { get; init; }
    
    public required int Priority { get; set; }

    public string? Url { get; set; }

    #region Navigations

    public DbManga? Manga { get; internal set; }

    #endregion
}