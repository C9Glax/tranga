using API.DTOs;
using Database.MangaContext;

namespace API.Helpers;

internal static class DbMetadataLinkHelpers
{
    public static MetadataLinkDTO ToDTO(this DbMetadataLink link) => new()
    {
        MangaId = link.MangaId,
        MetadataLinkId = link.Id,
        MetadataExtensionId = link.MetadataExtensionId,
        CoverFileId = link.CoverId,
        Status = link.Status,
        AgeRating = link.Rating,
        Demographic = link.Demographic,
        Url = link.Url,
        Description = link.Summary,
        Year = link.Year,
        Language = link.Language
    };
}