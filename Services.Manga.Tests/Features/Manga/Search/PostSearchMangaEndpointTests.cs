using Common.Datatypes;
using Common.Helpers;
using Common.Tests;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Manga.Search;

public class PostSearchMangaEndpointTests : TrangaTest
{
    [Fact]
    public async Task PostSearchManga_Rejects400WhenQueryHasNoUsableCriteria()
    {
        await using MangaContext context = MangaContextFactory.Create();
        PostSearchMangaEndpoint.PostSearchMangaRequest request = new(new SearchQuery(), null);

        Results<Ok<MetadataDto[]>, BadRequest, InternalServerError> result = await PostSearchMangaEndpoint.Handle(context, request, ct);

        Assert.IsType<BadRequest>(result.Result);
    }

    [Fact]
    public async Task PostSearchManga_SearchesRequestedMetadataExtensions()
    {
        await using MangaContext context = MangaContextFactory.Create();
        // A random, non-registered extension ID filters the extension set down to empty,
        // so the search runs (and returns) without making a real HTTP call to any extension.
        PostSearchMangaEndpoint.PostSearchMangaRequest request = new(
            new SearchQuery(Title: "One Piece"), [Guid.NewGuid()]);

        Results<Ok<MetadataDto[]>, BadRequest, InternalServerError> result = await PostSearchMangaEndpoint.Handle(context, request, ct);

        MetadataDto[] results = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindExistingMetadata_DoesNotMatchAcrossDifferentExtensionsWithSameSeries()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbMetadata mangaUpdatesEntry = TestDataBuilder.NewMetadata(series: "One Piece");
        mangaUpdatesEntry.Url = "https://mangaupdates.com/series/one-piece";
        await context.AddAsync(mangaUpdatesEntry, ct);
        await context.SaveChangesAsync(ct);

        SearchResult mangaDexResult = new()
        {
            MetadataExtensionIdentifier = Guid.NewGuid(),
            Identifier = Guid.NewGuid().ToString(),
            Cover = new TrangaImage(),
            Series = "One Piece",
            Url = "https://mangadex.org/title/one-piece"
        };

        DbMetadata? existing = await PostSearchMangaEndpoint.FindExistingMetadata(context, mangaDexResult, ct);

        Assert.Null(existing);
    }

    [Fact]
    public async Task FindExistingMetadata_MatchesSameExtensionBySeriesTitle()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbMetadata mangaUpdatesEntry = TestDataBuilder.NewMetadata(series: "One Piece");
        await context.AddAsync(mangaUpdatesEntry, ct);
        await context.SaveChangesAsync(ct);

        SearchResult sameExtensionResult = new()
        {
            MetadataExtensionIdentifier = mangaUpdatesEntry.MetadataExtension,
            Identifier = Guid.NewGuid().ToString(),
            Cover = new TrangaImage(),
            Series = "One Piece"
        };

        DbMetadata? existing = await PostSearchMangaEndpoint.FindExistingMetadata(context, sameExtensionResult, ct);

        Assert.NotNull(existing);
        Assert.Equal(mangaUpdatesEntry.MetadataId, existing.MetadataId);
    }

    [Fact]
    public async Task MergeMetadata_UpdatesFieldsFromNewSearchResult()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbFile cover = new() { FileId = Guid.NewGuid(), Path = "/covers", Name = "cover.jpg", MimeType = "image/jpeg" };
        DbMetadata existing = TestDataBuilder.NewMetadata(coverId: cover.FileId);
        existing.Summary = "Old Summary";
        existing.Year = 2000;
        await context.AddAsync(cover, ct);
        await context.AddAsync(existing, ct);
        await context.SaveChangesAsync(ct);

        SearchResult searchResult = new()
        {
            MetadataExtensionIdentifier = existing.MetadataExtension,
            Identifier = existing.Identifier,
            Cover = new TrangaImage(),
            Series = "Updated Series",
            Summary = "New Summary",
            Year = 2024,
            Url = "https://example.com/manga",
            Status = ReleaseStatus.Ongoing,
            NSFW = true,
            Genres = ["Action"],
            Artists = ["Some Artist"],
            Authors = ["Some Author"]
        };

        await PostSearchMangaEndpoint.MergeMetadata(context, searchResult, existing, ct);
        await context.SaveChangesAsync(ct);

        Assert.Equal("Updated Series", existing.Series);
        Assert.Equal("New Summary", existing.Summary);
        Assert.Equal(2024, existing.Year);
        Assert.Equal("https://example.com/manga", existing.Url);
        Assert.Equal(ReleaseStatus.Ongoing, existing.Status);
        Assert.True(existing.NSFW);
        Assert.Contains(existing.Genres!, g => g.Genre == "Action");
        Assert.Contains(existing.Artists!, a => a.Name == "Some Artist");
        Assert.Contains(existing.Authors!, a => a.Name == "Some Author");
    }

    [Fact]
    public async Task MergeMetadata_KeepsExistingValuesWhenNewResultHasNoData()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbFile cover = new() { FileId = Guid.NewGuid(), Path = "/covers", Name = "cover.jpg", MimeType = "image/jpeg" };
        DbMetadata existing = TestDataBuilder.NewMetadata(coverId: cover.FileId);
        existing.Summary = "Old Summary";
        existing.Year = 2000;
        await context.AddAsync(cover, ct);
        await context.AddAsync(existing, ct);
        await context.SaveChangesAsync(ct);

        SearchResult searchResult = new()
        {
            MetadataExtensionIdentifier = existing.MetadataExtension,
            Identifier = existing.Identifier,
            Cover = new TrangaImage(),
            Series = existing.Series
        };

        await PostSearchMangaEndpoint.MergeMetadata(context, searchResult, existing, ct);
        await context.SaveChangesAsync(ct);

        Assert.Equal("Old Summary", existing.Summary);
        Assert.Equal(2000, existing.Year);
    }
}
