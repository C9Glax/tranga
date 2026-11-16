using Common.Settings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetCustomDownloaderCapabilitiesEndpoint
{
    /// <summary>
    /// What this installation supports for custom Download-Sources
    /// </summary>
    /// <response code="200">The capabilities</response>
    public static Ok<CustomDownloaderCapabilities> Handle() =>
        TypedResults.Ok(new CustomDownloaderCapabilities(EnvVars.FlareSolverrUrl is not null, EnvVars.AllowPrivateSourceHosts));

    /// <summary>
    /// Used in <see cref="GetCustomDownloaderCapabilitiesEndpoint"/>
    /// </summary>
    /// <param name="FlareSolverrConfigured">A FlareSolverr instance is configured, so sources can use it</param>
    /// <param name="AllowPrivateHosts">Sources may fetch from loopback/LAN addresses (otherwise only public hosts)</param>
    public sealed record CustomDownloaderCapabilities(bool FlareSolverrConfigured, bool AllowPrivateHosts);
}
