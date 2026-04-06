using API.Entities.MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Features.Metadata;

public abstract class GetMetadataExtensionsEndpoint
{
    public static Ok<MetadataExtensionsList> Handle() => TypedResults.Ok(new MetadataExtensionsList());
}