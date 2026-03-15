namespace Common.Datatypes;

/// <summary>
/// The query to use when searching for a Manga
/// </summary>
/// <param name="Title">The title of the Manga.</param>
/// <param name="Tags"></param>
/// <param name="ContentRating"></param>
/// <param name="Year">The release-year of the Manga</param>
/// <param name="Author">An author of the Manga</param>
/// <param name="Artist">An artist of the Manga</param>
/// <param name="Language">The ISO language-code of a translation</param>
/// <param name="MangaUpdatesSeriesId">The MangaUpdates.com series id</param>
public sealed record SearchQuery(
    string? Title = null,
    string[]? Tags = null,
    ContentRating? ContentRating = null,
    int? Year = null,
    string? Author = null,
    string? Artist = null,
    string? Language = null,
    long? MangaUpdatesSeriesId = null
    );