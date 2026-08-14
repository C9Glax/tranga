using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Entities.DownloadExtensions;
using Services.Manga.Features.DownloadLinks;

namespace Services.Manga.Tests.Features.DownloadLinks;

public class GetDownloadExtensionsEndpointTests
{
    [Fact]
    public void GetDownloadExtensions_ReturnsAllExtensions()
    {
        Ok<DownloadExtensionsList> result = GetDownloadExtensionsEndpoint.Handle();

        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Extensions);
        // MangaDex is the only compiled-in download extension; everything else is discovered from the Suwayomi
        // sidecar at runtime, so it cannot be asserted on here.
        Assert.Contains(result.Value.Extensions, e => e.Name == "MangaDex");
        Assert.All(result.Value.Extensions, e => Assert.False(string.IsNullOrEmpty(e.IconUrl)));
    }
}
