using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Entities.MetadataExtensions;
using Services.Manga.Features.Metadata;

namespace Services.Manga.Tests.Features.Metadata;

public class GetMetadataExtensionsEndpointTests
{
    [Fact]
    public void GetMetadataExtensions_ReturnsAllExtensions()
    {
        Ok<MetadataExtensionsList> result = GetMetadataExtensionsEndpoint.Handle();

        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Extensions);
        Assert.Contains(result.Value.Extensions, e => e.Name == "MangaDex");
        Assert.Contains(result.Value.Extensions, e => e.Name == "MangaUpdates");
        Assert.Contains(result.Value.Extensions, e => e.Name == "AniList");
        Assert.Contains(result.Value.Extensions, e => e.Name == "MyAnimeList");
    }
}
