using Services.Manga.Entities;
using Database.MangaContext;

namespace Services.Manga.Helpers;

internal static class MangaDTOHelper
{
    public static Entities.Manga ToDTO(this DbMangaMetadataEntries source) => new()
    {
        MangaId = source.MangaId,
        Monitored = source.Manga.Monitored,
        MetadataEntry = source.Metadata.ToDTO() with
        {
            Chosen = source.Chosen
        }
    };
}