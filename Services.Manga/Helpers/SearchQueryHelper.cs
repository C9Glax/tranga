using Common.Datatypes;
using Extensions.Extensions;
using Services.Manga.Database;

namespace Services.Manga.Helpers;

/// <summary>
/// Conversion helpers for building a <see cref="SearchQuery"/> from stored manga metadata.
/// </summary>
public static class SearchQueryHelper
{
    private static readonly Guid MangaDexExtensionId = new MangaDex().Identifier;
    private static readonly Guid MangaUpdatesExtensionId = new MangaUpdates().Identifier;
    private static readonly Guid AniListExtensionId = new AniList().Identifier;
    private static readonly Guid MyAnimeListExtensionId = new MyAnimeList().Identifier;

    /// <summary>
    /// Builds a <see cref="SearchQuery"/> from a manga's metadata entry, seeded with its series title. When the
    /// metadata came from an extension that supports exact ID lookup (MangaDex, MangaUpdates, AniList, MyAnimeList), the extension-native
    /// series ID is included too, so a later search pins the same entry instead of re-running an independent fuzzy
    /// title search that can drift to a different, similarly-titled manga.
    /// </summary>
    public static SearchQuery ToSearchQuery(this DbMangaMetadataEntries source)
    {
        DbMetadata metadata = source.Metadata;

        Guid? mangaDexSeriesId = metadata.MetadataExtension == MangaDexExtensionId &&
            Guid.TryParse(metadata.Identifier, out Guid mangaDexId) ? mangaDexId : null;

        long? mangaUpdatesSeriesId = metadata.MetadataExtension == MangaUpdatesExtensionId &&
            long.TryParse(metadata.Identifier, out long mangaUpdatesId) ? mangaUpdatesId : null;

        int? aniListSeriesId = metadata.MetadataExtension == AniListExtensionId &&
            int.TryParse(metadata.Identifier, out int aniListId) ? aniListId : null;

        int? myAnimeListSeriesId = metadata.MetadataExtension == MyAnimeListExtensionId &&
            int.TryParse(metadata.Identifier, out int myAnimeListId) ? myAnimeListId : null;

        return new()
        {
            Title = metadata.Series,
            MangaDexSeriesId = mangaDexSeriesId,
            MangaUpdatesSeriesId = mangaUpdatesSeriesId,
            AniListSeriesId = aniListSeriesId,
            MyAnimeListSeriesId = myAnimeListSeriesId
        };
    }
}
