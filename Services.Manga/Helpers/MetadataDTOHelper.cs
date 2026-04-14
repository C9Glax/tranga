using Services.Manga.Database;
using Services.Manga.Entities;

namespace Services.Manga.Helpers;

internal static class MetadataDTOHelper
{
    public static Metadata ToDTO(this DbMetadata metadata) => new()
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