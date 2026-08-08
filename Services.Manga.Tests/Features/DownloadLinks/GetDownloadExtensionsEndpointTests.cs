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
        Assert.Contains(result.Value.Extensions, e => e.Name == "MangaDex");
    }
}
