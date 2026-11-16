using Common.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Features.CustomDownloaders;

namespace Services.Manga.Tests.Features.CustomDownloaders;

public class GetCustomDownloaderCapabilitiesEndpointTests
{
    [Fact]
    public void Capabilities_ReflectTheEnvironment()
    {
        Ok<GetCustomDownloaderCapabilitiesEndpoint.CustomDownloaderCapabilities> result = GetCustomDownloaderCapabilitiesEndpoint.Handle();

        Assert.Equal(EnvVars.FlareSolverrUrl is not null, result.Value!.FlareSolverrConfigured);
        Assert.Equal(EnvVars.AllowPrivateSourceHosts, result.Value.AllowPrivateHosts);
    }
}
