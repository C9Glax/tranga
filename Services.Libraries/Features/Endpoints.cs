using Common.Services;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Features.ServiceDirectoryMappings;

namespace Services.Libraries.Features;

public sealed class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Libraries").ConfigureLibrariesEndpoints();

        builder.MapGroup("/directoryMappings")
            .WithTags("Libraries", "Files").ConfigureMappingEndpoints();
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
        
        builder.MapDelete("{libraryId}", DeleteLibraryEndpoint.Handle)
            .WithSummary("Remove a library extension");
    }
    
    
    internal static void ConfigureMappingEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPut("{libraryId}", AddServiceDirectoryMapping.Handle)
            .WithSummary("Add a directory mapping for a library");
        
        builder.MapGet("{libraryId}", GetServiceDirectoryMappings.Handle)
            .WithSummary("List of all mappings for a library extension");
        
        builder.MapDelete("{mappingId}", DeleteServiceDirectoryMapping.Handle)
            .WithSummary("Remove a mapping");
    }
}