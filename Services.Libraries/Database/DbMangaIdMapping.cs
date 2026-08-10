using Extensions.Data;

namespace Services.Libraries.Database;

/// <summary>
/// Maps a Manga (identified in the Manga service) to its corresponding series in a library extension
/// (e.g. a Komga series).
/// </summary>
public sealed record DbMangaIdMapping(Guid LibraryServiceId, Guid MangaId, string SeriesId)
{

    /// <summary>
    /// Id of the library connection (<see cref="DbLibraryService.LibraryServiceId"/>) this mapping belongs to.
    /// </summary>
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