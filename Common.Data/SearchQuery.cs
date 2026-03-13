namespace Common.Data;

public sealed record SearchQuery(
    string? Title = null,
    string[]? Tags = null,
    ContentRating? ContentRating = null,
    int? Year = null,
    string? Author = null,
    string? Artist = null,
    Language? Language = null,
    int? MangaUpdatesSeriesId = null,
    int? AniListId = null,
    int? MyAnimeListId = null
    );