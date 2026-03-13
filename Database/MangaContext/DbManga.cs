using Data;

namespace Database.MangaContext;

public sealed record DbManga : IRef
{
    public Guid MangaId { get; init; }
    
    public int? MangaUpdatesSeriesId { get; init; }
    
    public int? AniListId { get; init; }
    
    public int? MyAnimeListId { get; init; }
    
    public ICollection<ExtensionId<DbManga>>? ExtensionIds { get; init; }
    
    public ICollection<DbChapter>? Chapters { get; init; }
    
    public ComicInfo? ComicInfo { get; init; }
}