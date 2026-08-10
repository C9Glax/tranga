using Common.Datatypes;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

/// <summary>
/// Conversion helpers for building a <see cref="SearchQuery"/> from stored manga metadata.
/// </summary>
public static class SearchQueryHelper
{
    /// <summary>Builds a <see cref="SearchQuery"/> from a manga's metadata entry, seeded with its series title.</summary>
    public static SearchQuery ToSearchQuery(this DbMangaMetadataEntries source) => new()
    {
        Title = source.Metadata.Series
        //TODO Add more fields
    };
}
