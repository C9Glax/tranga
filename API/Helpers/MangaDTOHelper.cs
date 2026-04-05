using API.Entities;
using Database.MangaContext;

namespace API.Helpers;

internal static class MangaDTOHelper
{
    public static MangaMetadata ToDTO(this DbMetadataSource metadata) => new()
    {
        MangaId = metadata.MangaId,
        Series = metadata.Series,
        Summary = metadata.Summary,
        Year = metadata.Year,
        Language = metadata.Language,
        ChaptersNumber = metadata.ChaptersNumber,
        CoverId = metadata.CoverId,
        MetadataExtensionId = metadata.MetadataExtension,
        Identifier = metadata.Identifier,
        Url = metadata.Url,
        Monitored = metadata.Manga?.Monitored
    };
}