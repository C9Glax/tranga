using Common.Services;
using Services.Libraries.Features.Libraries;

namespace Services.Libraries.Features;

public sealed class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Libraries").ConfigureLibrariesEndpoints();
    }
}

internal static class EndpointHelpers
{
    internal static void ConfigureLibrariesEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetLibrariesEndpoint.Handle)
            .WithSummary("List of all configured library extensions");
        
        builder.MapPut("/komga", AddKomgaEndpoint.Handle)
            .WithSummary("Add komga library extension");

        builder.MapPost("/komga/test-connection", TestKomgaConnectionEndpoint.Handle)
            .WithSummary("Validate Komga credentials without persisting anything");

        builder.MapDelete("{libraryId}", DeleteLibraryEndpoint.Handle)
            .WithSummary("Remove a library extension");

        builder.MapPatch("{libraryId}", PatchLibraryEndpoint.Handle)
            .WithSummary("Rename a library extension and/or rotate its credentials");

        builder.MapGet("/mappings/{mangaId}", GetLibraryMangaLinkEndpoint.Handle)
            .WithSummary("Get the library-extension links for a Manga");
    }
}