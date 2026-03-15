namespace Common.Datatypes;

public sealed record SearchQuery(
    string? Title = null,
    string[]? Tags = null,
    ContentRating? ContentRating = null,
    int? Year = null,
    string? Author = null,
    string? Artist = null,
    Language? Language = null,
    long? MangaUpdatesSeriesId = null,
    int? AniListId = null,
    int? MyAnimeListId = null
    );