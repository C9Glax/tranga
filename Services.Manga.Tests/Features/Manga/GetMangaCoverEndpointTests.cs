using System.Text;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

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
