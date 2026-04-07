using API.Entities;
using Database.MangaContext;

namespace API.Helpers;

internal static class MangaDTOHelper
{
    public static Manga ToDTO(this DbMangaMetadataEntries source) => new()
    {
        MangaId = source.MangaId,
        Monitored = source.Manga.Monitored,
        MetadataEntry = source.Metadata.ToDTO() with
        {
            Chosen = source.Chosen
        }
    };
}