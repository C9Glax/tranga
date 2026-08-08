using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Features.Chapters;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Chapters;

// NOTE: despite the route being a GET, GetChaptersEndpoint filters by an explicit list of
// Chapter-IDs supplied in the request body, rather than returning every Chapter or filtering by
// Manga-ID as the backlog's "GET /mangas/chapters returns all chapters" wording suggests. Tests
// below reflect the actual (ID-list-filtered) contract.
public class GetChaptersEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetChapters_ReturnsChaptersMatchingRequestedIds()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapterA = await TestDataBuilder.SeedChapter(context, manga, number: "1", ct: ct);
        DbChapter chapterB = await TestDataBuilder.SeedChapter(context, manga, number: "2", ct: ct);
        await TestDataBuilder.SeedChapter(context, manga, number: "3", ct: ct);

        Ok<Chapter[]> result = await GetChaptersEndpoint.Handle(context, [chapterA.ChapterId, chapterB.ChapterId], ct);

        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Length);
        Assert.Contains(result.Value, c => c.ChapterId == chapterA.ChapterId);
        Assert.Contains(result.Value, c => c.ChapterId == chapterB.ChapterId);
    }

    [Fact]
    public async Task GetChapters_ReturnsEmptyArrayForUnknownIds()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Ok<Chapter[]> result = await GetChaptersEndpoint.Handle(context, [Guid.NewGuid()], ct);

        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetChapters_ReturnsEmptyArrayWhenNoIdsProvided()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        await TestDataBuilder.SeedChapter(context, manga, ct: ct);

        Ok<Chapter[]> result = await GetChaptersEndpoint.Handle(context, [], ct);

        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
