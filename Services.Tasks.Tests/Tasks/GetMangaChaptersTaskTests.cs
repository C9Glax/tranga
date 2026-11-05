using Common.Tests;
using Extensions.Data;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;

namespace Services.Tasks.Tests.Tasks;

public class GetMangaChaptersTaskTests : TrangaTest
{
    private static readonly Guid Extension = Guid.CreateVersion7();

    private static ChapterInfo Info(string number, string identifier, string? title = null) =>
        new(Extension, number, $"https://example.org{identifier}", identifier, Title: title);

    private async Task<(MangaContext Ctx, DbManga Manga)> Setup()
    {
        MangaContext ctx = MangaContextFactory.Create();
        DbManga manga = new() { Monitored = true };
        await ctx.Mangas.AddAsync(manga, ct);
        await ctx.SaveChangesAsync(ct);
        return (ctx, manga);
    }

    private async Task<(int Chapters, int Links, int Skipped)> Merge(MangaContext ctx, DbManga manga, params ChapterInfo[] infos)
    {
        (int chapters, int links, int skipped) =
            await GetMangaChaptersTask.MergeChaptersAsync(ctx, manga, infos, NoOpLogger.Instance, ct);
        await ctx.SaveChangesAsync(ct);
        return (chapters, links, skipped);
    }

    [Fact]
    public async Task MergeChaptersAsync_AddsNewChapters()
    {
        (MangaContext ctx, DbManga manga) = await Setup();

        Assert.Equal((2, 0, 0), await Merge(ctx, manga, Info("1", "/a"), Info("2", "/b")));
        Assert.Equal(2, await ctx.Chapters.CountAsync(ct));
    }

    [Fact]
    public async Task MergeChaptersAsync_SkipsExistingLinks()
    {
        (MangaContext ctx, DbManga manga) = await Setup();
        await Merge(ctx, manga, Info("1", "/a"));

        Assert.Equal((1, 0, 1), await Merge(ctx, manga, Info("1", "/a"), Info("2", "/b")));
        Assert.Equal(2, await ctx.Chapters.CountAsync(ct));
    }

    [Fact]
    public async Task MergeChaptersAsync_SameNumberFromSameExtension_CreatesSeparateChapter()
    {
        // e.g. an "ex" chapter without a number falling back to a number that is already taken by a real chapter.
        (MangaContext ctx, DbManga manga) = await Setup();
        await Merge(ctx, manga, Info("5", "/five"));

        // used to violate the (ChapterId, DownloadExtension) key
        Assert.Equal((2, 0, 0), await Merge(ctx, manga, Info("5", "/ex", "ex"), Info("6", "/six")));
        Assert.Equal(3, await ctx.Chapters.CountAsync(ct));
        Assert.Equal(3, await ctx.ChapterDownloadLinks.CountAsync(ct));
    }

    [Fact]
    public async Task MergeChaptersAsync_DuplicateNumbersWithinBatch_CreatesSeparateChapters()
    {
        (MangaContext ctx, DbManga manga) = await Setup();

        Assert.Equal((3, 0, 0),
            await Merge(ctx, manga, Info("3", "/three"), Info("3", "/ex1", "ex"), Info("3", "/ex2", "ex")));
        Assert.Equal(3, await ctx.Chapters.CountAsync(ct));
    }

    [Fact]
    public async Task MergeChaptersAsync_SameNumberFromOtherExtension_AttachesLinkToExistingChapter()
    {
        (MangaContext ctx, DbManga manga) = await Setup();
        await Merge(ctx, manga, Info("1", "/a"));

        ChapterInfo other = new(Guid.CreateVersion7(), "1", "https://other.org/1", "/other1");
        Assert.Equal((0, 1, 0), await Merge(ctx, manga, other));
        Assert.Equal(1, await ctx.Chapters.CountAsync(ct));
        Assert.Equal(2, await ctx.ChapterDownloadLinks.CountAsync(ct));
    }
}
