using API.Entities;
using Database.MangaContext;

namespace API.Helpers;

internal static class MangaDTOHelper
{
    public static Manga ToDTO(this DbMangaMetadataSource source) => new()
    {
        MangaId = source.MangaId,
        Monitored = source.Manga.Monitored,
        MetadataEntry = source.MetadataSource.ToDTO() with
        {
            Chosen = source.Chosen
        }
    };
    
    public static Manga ToDTO(this DbManga manga) => new()
    {
        MangaId = manga.MangaId,
        Monitored = manga.Monitored,
        MetadataEntry = manga.MetadataSources?.FirstOrDefault(m => m.Chosen == true)?.MetadataSource?.ToDTO()
    };
    
    public static Metadata ToDTO(this DbMetadataSource metadata) => new()
    {
        MetadataId = metadata.MetadataId,
        Series = metadata.Series,
        Summary = metadata.Summary,
        Year = metadata.Year,
        Language = metadata.Language,
        ChaptersNumber = metadata.ChaptersNumber,
        CoverId = metadata.CoverId,
        MetadataExtensionId = metadata.MetadataExtension,
        Identifier = metadata.Identifier,
        Status = metadata.Status,
        Url = metadata.Url,
        NSFW = metadata.NSFW
    };
}