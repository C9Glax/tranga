using API.Entities;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Metadata;

public abstract class GetMetadataEndpoint
{
    public static async Task<Results<Ok<MetadataMangaIds>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid metadataId, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataSources
                .Where(s => s.MetadataSourceId == metadataId)
                .GroupBy(s => s.MetadataSource)
                .Select(s => new
                {
                    Metadata = s.Key,
                    MangaIds = s.Select(ms => ms.MangaId),
                    Chosen = s.Any(ms => ms.Chosen)
                })
                .FirstOrDefaultAsync(ct) is not { } queryResult)
        {
            return TypedResults.NotFound();
        }

        MetadataMangaIds result = new  ()
        {
            MetadataId = queryResult.Metadata.MetadataId,
            Series = queryResult.Metadata.Series,
            Summary = queryResult.Metadata.Summary,
            Year = queryResult.Metadata.Year,
            Language = queryResult.Metadata.Language,
            ChaptersNumber = queryResult.Metadata.ChaptersNumber,
            CoverId = queryResult.Metadata.CoverId,
            MetadataExtensionId = queryResult.Metadata.MetadataExtension,
            Identifier = queryResult.Metadata.Identifier,
            Url = queryResult.Metadata.Url,
            Chosen = queryResult.Chosen,
            MangaIds = queryResult.MangaIds.ToArray(),
            NSFW = queryResult.Metadata.NSFW
        };
        return TypedResults.Ok(result);
    }
}