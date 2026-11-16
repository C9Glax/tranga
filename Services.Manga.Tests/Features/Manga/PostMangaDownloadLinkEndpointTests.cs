using Common.Tests;
using Extensions.Data;
using Extensions.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Features.Manga;

public class PostMangaDownloadLinkEndpointTests : TrangaTest, IDisposable
{
    // The cases that reach the extension use a FakeDownloadExtension, registered through a provider
    // (DownloadExtensionsCollection.RegisterProvider): the real extensions would call their websites.

    private readonly string _mangaCoverDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", Guid.NewGuid().ToString("N"));
    private readonly List<DbFile> _storedCovers = [];

    public void Dispose()
    {
        // The covers of the links are stored in the (relative) cover directory of the service.
        foreach (DbFile file in _storedCovers)
            file.DeleteFile();
        if (Directory.Exists(_mangaCoverDirectory))
            Directory.Delete(_mangaCoverDirectory, recursive: true);
    }

    private async Task<FakeDownloadExtension> RegisterExtension(FakeDownloadExtension extension)
    {
        await FakeDownloadExtension.Register(extension, ct);
        return extension;
    }

    /// <summary>A manga whose chosen metadata has a cover (a file of its own, with the bytes of a JPEG), if wanted.</summary>
    private async Task<(DbManga Manga, DbFile? Cover)> SeedManga(MangaContext context, bool withCover)
    {
        DbFile? cover = null;
        if (withCover)
        {
            cover = new DbFile { FileId = Guid.NewGuid(), Path = _mangaCoverDirectory, Name = "cover.jpg", MimeType = "image/jpeg" };
            await cover.SaveFile(FakeDownloadExtension.Jpeg(), ct);
            await context.AddAsync(cover, ct);
            await context.SaveChangesAsync(ct);
        }

        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, coverId: cover?.FileId, ct: ct);
        return (manga, cover);
    }

    private async Task<MangaDownloadLinkDto> Add(MangaContext context, DbManga manga, FakeDownloadExtension extension)
    {
        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(extension.Identifier, $"{FakeDownloadExtension.SeriesUrlPrefix}7/some-series"), ct);

        MangaDownloadLinkDto dto = Assert.IsType<Ok<MangaDownloadLinkDto>>(result.Result).Value!;
        if (dto.CoverId is { } coverId)
            _storedCovers.Add(await context.Files.AsNoTracking().SingleAsync(f => f.FileId == coverId, ct));
        return dto;
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, Guid.NewGuid(), new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(Guid.NewGuid(), "https://example.com/foo"), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns400ForUnknownExtension()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId, new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(Guid.NewGuid(), "https://example.com/foo"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns400ForUrlNotMatchingExtension()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(new MangaDex().Identifier, "https://example.com/foo"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_DuplicateLink_SetsMonitoredAndReturnsOk()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, monitored: false, ct: ct);
        Assert.False(manga.Monitored);

        Guid extensionId = new MangaDex().Identifier;
        Guid seriesGuid = Guid.NewGuid();

        DbDownloadLink downloadLink = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = extensionId,
            Identifier = seriesGuid.ToString(),
            Series = "Series",
            Url = $"https://mangadex.org/title/{seriesGuid}"
        };
        DbMangaDownloadLinks link = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = downloadLink.DownloadLinkId,
            Matched = false,
            Priority = 0,
            Manga = manga,
            DownloadLink = downloadLink
        };
        await context.AddAsync(downloadLink, ct);
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(extensionId, $"https://mangadex.org/title/{seriesGuid}"), ct);

        MangaDownloadLinkDto dto = Assert.IsType<Ok<MangaDownloadLinkDto>>(result.Result).Value!;
        Assert.Equal(downloadLink.DownloadLinkId, dto.DownloadId);
        Assert.True(manga.Monitored);
    }

    [Fact]
    public async Task PostMangaDownloadLink_NewLink_GetsTheCoverOfTheExtension()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { HasCover = true });
        (DbManga manga, _) = await SeedManga(context, withCover: false);

        MangaDownloadLinkDto dto = await Add(context, manga, extension);

        // the manga has no cover of its own, so this one can only come from the extension
        Assert.NotNull(dto.CoverId);
        Assert.Equal(1, extension.CoverRequests);
        DbDownloadLink stored = (await context.MangaDownloadLinks.SingleAsync(l => l.DownloadLinkId == dto.DownloadId, ct)).DownloadLink;
        Assert.Equal(dto.CoverId, stored.CoverId);
        DbFile file = await context.Files.SingleAsync(f => f.FileId == dto.CoverId, ct);
        Assert.Equal("image/jpeg", file.MimeType);
        Assert.True(System.IO.File.Exists(file.FullPath));
    }

    [Fact]
    public async Task PostMangaDownloadLink_ExtensionWithoutCover_FallsBackToACopyOfTheMangasCover()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { HasCover = false });
        (DbManga manga, DbFile? mangaCover) = await SeedManga(context, withCover: true);

        MangaDownloadLinkDto dto = await Add(context, manga, extension);

        Assert.NotNull(dto.CoverId);
        // a file of its own (a file belongs to one metadata or link), with the same content
        Assert.NotEqual(mangaCover!.FileId, dto.CoverId);
        DbFile copy = await context.Files.SingleAsync(f => f.FileId == dto.CoverId, ct);
        Assert.Equal(await System.IO.File.ReadAllBytesAsync(mangaCover.FullPath, ct), await System.IO.File.ReadAllBytesAsync(copy.FullPath, ct));
    }

    [Fact]
    public async Task PostMangaDownloadLink_ExtensionFailingToGetTheCover_StillAddsTheLinkWithTheMangasCover()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { CoverThrows = true });
        (DbManga manga, DbFile? mangaCover) = await SeedManga(context, withCover: true);

        MangaDownloadLinkDto dto = await Add(context, manga, extension);

        Assert.Equal(1, extension.CoverRequests);
        Assert.NotNull(dto.CoverId);
        Assert.NotEqual(mangaCover!.FileId, dto.CoverId);
    }

    [Fact]
    public async Task PostMangaDownloadLink_WithoutAnyCover_AddsTheLinkWithoutOne()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { HasCover = false });
        (DbManga manga, _) = await SeedManga(context, withCover: false);

        MangaDownloadLinkDto dto = await Add(context, manga, extension);

        Assert.Null(dto.CoverId);
        Assert.Equal($"{FakeDownloadExtension.SeriesUrlPrefix}7/some-series", dto.Identifier);
    }

    [Fact]
    public async Task PostMangaDownloadLink_ChaptersCannotBeFetched_Returns400AndAddsNothing()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { Chapters = null });
        (DbManga manga, _) = await SeedManga(context, withCover: false);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(extension.Identifier, $"{FakeDownloadExtension.SeriesUrlPrefix}7/some-series"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Equal(0, extension.CoverRequests);
        Assert.Empty(await context.MangaDownloadLinks.Where(l => l.MangaId == manga.MangaId).ToListAsync(ct));
    }

    [Fact]
    public async Task PostMangaDownloadLink_ExistingLinkWithoutCover_GetsTheCover()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { HasCover = true });
        (DbManga manga, _) = await SeedManga(context, withCover: false);
        MangaDownloadLinkDto first = await Add(context, manga, extension);
        // links that were added before covers were fetched have none
        await context.DownloadLinks.ExecuteUpdateAsync(l => l.SetProperty(d => d.CoverId, (Guid?)null), ct);
        context.ChangeTracker.Clear();
        Assert.NotNull(first.CoverId);

        MangaDownloadLinkDto second = await Add(context, manga, extension);

        Assert.Equal(first.DownloadId, second.DownloadId);
        Assert.NotNull(second.CoverId);
    }

    [Fact]
    public async Task PostMangaDownloadLink_ExistingLinkWithCover_IsLeftAlone()
    {
        await using MangaContext context = MangaContextFactory.Create();
        FakeDownloadExtension extension = await RegisterExtension(new FakeDownloadExtension { HasCover = true });
        (DbManga manga, _) = await SeedManga(context, withCover: false);
        MangaDownloadLinkDto first = await Add(context, manga, extension);
        Assert.Equal(1, extension.CoverRequests);

        MangaDownloadLinkDto second = await Add(context, manga, extension);

        Assert.Equal(first.DownloadId, second.DownloadId);
        Assert.Equal(first.CoverId, second.CoverId);
        Assert.Equal(1, extension.CoverRequests);
    }
}
