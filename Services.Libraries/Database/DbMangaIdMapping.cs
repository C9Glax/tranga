using Extensions.Data;

namespace Services.Libraries.Database;

public sealed record DbMangaIdMapping(Guid LibraryServiceId, Guid MangaId, string SeriesId)
{

    public Guid LibraryServiceId { get; init; } = LibraryServiceId;
    
    /// <summary>
    /// Id in the Manga Service
    /// </summary>
    public Guid MangaId { get; init; } = MangaId;

    /// <summary>
    /// Id in the library extension
    /// </summary>
    public string SeriesId { get; init; } = SeriesId;
    
    #region Navigations

    internal DbLibraryService? LibraryService { get; init; }

    #endregion
}