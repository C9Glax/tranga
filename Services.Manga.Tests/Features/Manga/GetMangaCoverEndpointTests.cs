using System.Text;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaCoverEndpointTests : TrangaTest, IDisposable
{
    private readonly string _coverDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_coverDirectory))
            Directory.Delete(_coverDirectory, recursive: true);
    }

    [Fact]
    public async Task GetMangaCover_ReturnsCoverImage()
    {
        await using MangaContext context = MangaContextFactory.Create();
        byte[] content = "cover-bytes"u8.ToArray();
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _coverDirectory, Name = "cover.jpg", MimeType = "image/jpeg" };
        await file.SaveFile(new MemoryStream(content), ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, coverId: file.FileId, ct: ct);

        Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError> result =
            await GetMangaCoverEndpoint.Handle(context, manga.MangaId, ct);

        FileStreamHttpResult fileResult = Assert.IsType<FileStreamHttpResult>(result.Result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        using MemoryStream buffer = new();
        await fileResult.FileStream.CopyToAsync(buffer, ct);
        Assert.Equal(content, buffer.ToArray());
    }

    [Fact]
    public async Task GetMangaCover_ResizesAndCachesWhenDimensionsGiven()
    {
        await using MangaContext context = MangaContextFactory.Create();
        using Image<Rgba32> source = new(40, 20);
        MemoryStream sourceStream = new();
        source.SaveAsJpeg(sourceStream);
        DbFile file = new() { FileId = Guid.NewGuid(), Path = _coverDirectory, Name = "cover.jpg", MimeType = "image/jpeg" };
        await file.SaveFile(sourceStream, ct);
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);

        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, coverId: file.FileId, ct: ct);

        Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError> result =
            await GetMangaCoverEndpoint.Handle(context, manga.MangaId, ct, width: 10, height: 10);

        FileStreamHttpResult fileResult = Assert.IsType<FileStreamHttpResult>(result.Result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        using MemoryStream buffer = new();
        await fileResult.FileStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        ImageInfo info = await Image.IdentifyAsync(buffer, ct);
        Assert.Equal(10, info.Width);
        Assert.Equal(10, info.Height);

        string cachePath = Path.Combine(_coverDirectory, "cache", "cover_10x10.jpg");
        Assert.True(System.IO.File.Exists(cachePath));

        Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError> cachedResult =
            await GetMangaCoverEndpoint.Handle(context, manga.MangaId, ct, width: 10, height: 10);
        FileStreamHttpResult cachedFileResult = Assert.IsType<FileStreamHttpResult>(cachedResult.Result);
        using MemoryStream cachedBuffer = new();
        await cachedFileResult.FileStream.CopyToAsync(cachedBuffer, ct);
        Assert.Equal(buffer.ToArray(), cachedBuffer.ToArray());
    }

    [Fact]
    public async Task GetMangaCover_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError> result =
            await GetMangaCoverEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetMangaCover_ReturnsNoContentWhenNoCoverIsSet()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError> result =
            await GetMangaCoverEndpoint.Handle(context, manga.MangaId, ct);

        Assert.IsType<NoContent>(result.Result);
    }
}
