using API.Entities;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Metadata;

public class GetMetadataListEndpoint
{
    public static async Task<Results<Ok<MetadataMangaIds[]>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataSources
                .GroupBy(s => s.MetadataSource)
                .Select(s => new
                {
                    Metadata = s.Key,
                    MangaIds = s.Select(ms => ms.MangaId)
                })
                .ToListAsync(ct) is not { } metadata)
        {
            return TypedResults.InternalServerError();
        }

        MetadataMangaIds[] result = metadata.Select(s => new MetadataMangaIds ()
        {
            MetadataId = s.Metadata.MetadataId,
            Series = s.Metadata.Series,
            Summary = s.Metadata.Summary,
            Year = s.Metadata.Year,
            Language = s.Metadata.Language,
            ChaptersNumber = s.Metadata.ChaptersNumber,
            CoverId = s.Metadata.CoverId,
            MetadataExtensionId = s.Metadata.MetadataExtension,
            Identifier = s.Metadata.Identifier,
            Url = s.Metadata.Url,
            MangaIds = s.MangaIds.ToArray(),
            NSFW = s.Metadata.NSFW
        }).ToArray();
        return TypedResults.Ok(result);
    }
}