using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Features.Chapters;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Chapters;

public class GetChapterEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetChapter_ReturnsSpecificChapterById()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, number: "12", ct: ct);

        Results<Ok<Chapter>, NotFound> result = await GetChapterEndpoint.Handle(context, chapter.ChapterId, ct);

        Chapter dto = Assert.IsType<Ok<Chapter>>(result.Result).Value!;
        Assert.Equal(chapter.ChapterId, dto.ChapterId);
        Assert.Equal(manga.MangaId, dto.MangaId);
        Assert.Equal("12", dto.Number);
    }

    [Fact]
    public async Task GetChapter_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<Chapter>, NotFound> result = await GetChapterEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
