using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Metadata;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Metadata;

public class GetMetadataEntriesEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMetadataEntries_ReturnsAllEntries()
    {
        await using MangaContext context = MangaContextFactory.Create();
        await context.AddAsync(TestDataBuilder.NewMetadata("Series A"), ct);
        await context.AddAsync(TestDataBuilder.NewMetadata("Series B"), ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MetadataDto[]>, InternalServerError> result = await GetMetadataEntriesEndpoint.Handle(context, ct);

        MetadataDto[] entries = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Equal(2, entries.Length);
    }

    [Fact]
    public async Task GetMetadataEntries_ReturnsEmptyWhenNoneExist()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MetadataDto[]>, InternalServerError> result = await GetMetadataEntriesEndpoint.Handle(context, ct);

        MetadataDto[] entries = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Empty(entries);
    }
}
