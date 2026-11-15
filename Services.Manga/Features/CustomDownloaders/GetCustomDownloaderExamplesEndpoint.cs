using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetCustomDownloaderExamplesEndpoint
{
    /// <summary>
    /// Ready-made custom Download-Source definitions to start from (their IDs are empty, a new one is assigned when they are created)
    /// </summary>
    /// <response code="200">The example definitions</response>
    public static Ok<List<SelectorDownloaderDefinition>> Handle() =>
        TypedResults.Ok(SelectorDownloaderJson.Examples().ToList());
}
