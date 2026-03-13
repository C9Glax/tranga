using Extensions.Helpers;

namespace Extensions.Data;

public sealed record SearchQuery(
    string? Title = null,
    string[]? Tags = null,
    ContentRating? ContentRating = null,
    int? Year = null,
    string? Author = null,
    string? Artist = null,
    Language? Language = null
    );