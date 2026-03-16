using Common.Datatypes;

namespace Database.MangaContext;

public sealed record DbManga
{
    public Guid MangaId { get; init; }
    
    public long? MangaUpdatesSeriesId { get; init; }
    
    public ICollection<DownloadExtensionId<DbManga>>? DownloadExtensionIds { get; init; }
    
    public ICollection<DbChapter>? Chapters { get; init; }
    
    public required string Title { get; set; }
    public required string? Description { get; set; }
    public required int? Year { get; set; }
    public required string[]? Authors { get; set; }
    public required string[]? Artists { get; set; }
    public required string[]? Genre { get; set; }
    public required string[]? Tags { get; set; }
    public required AgeRating? AgeRating { get; set; }
}