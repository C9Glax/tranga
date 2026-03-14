using API.Features;

namespace API;

internal static class Endpoints
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapPost("/search", PostSearchMangaEndpoint.Handle)
            .WithDisplayName("Search")
            .WithSummary("Searches for a Manga.")
            .WithTags("Search");
    }
}