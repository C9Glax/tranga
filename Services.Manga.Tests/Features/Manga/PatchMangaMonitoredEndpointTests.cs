using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PatchMangaMonitoredEndpointTests : TrangaTest
{
    [Fact]
    public async Task PatchMangaMonitored_SetsMonitoredTrue()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga(monitored: false);
        await context.Mangas.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok, NotFound> result = await PatchMangaMonitoredEndpoint.Handle(
            context, manga.MangaId, new PatchMangaMonitoredEndpoint.PatchMangaMonitoredRequest(Monitored: true), ct);

        Assert.IsType<Ok>(result.Result);
        Assert.True((await context.Mangas.SingleAsync(m => m.MangaId == manga.MangaId, ct)).Monitored);
    }

    [Fact]
    public async Task PatchMangaMonitored_SetsMonitoredFalse()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga(monitored: true);
        await context.Mangas.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok, NotFound> result = await PatchMangaMonitoredEndpoint.Handle(
            context, manga.MangaId, new PatchMangaMonitoredEndpoint.PatchMangaMonitoredRequest(Monitored: false), ct);

        Assert.IsType<Ok>(result.Result);
        Assert.False((await context.Mangas.SingleAsync(m => m.MangaId == manga.MangaId, ct)).Monitored);
    }

    [Fact]
    public async Task PatchMangaMonitored_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok, NotFound> result = await PatchMangaMonitoredEndpoint.Handle(
            context, Guid.NewGuid(), new PatchMangaMonitoredEndpoint.PatchMangaMonitoredRequest(Monitored: true), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
