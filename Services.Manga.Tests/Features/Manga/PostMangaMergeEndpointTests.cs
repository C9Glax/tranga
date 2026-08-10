using Common.Services.Events;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PostMangaMergeEndpointTests : TrangaTest
{
    private static readonly PostMangaMergeEndpoint.PostMangaMergeRequest KeepTargetDefaults = new(Guid.Empty, false, false);

    private static EventPublisher CreatePublisher()
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.SetupGet(c => c.IsOpen).Returns(true);
        return new EventPublisher(mockChannel.Object);
    }

    [Fact]
    public async Task Merge_KeepsTargetMetadataByDefault_UnionsUnchosenCandidates()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, DbMetadata targetMetadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Target", ct: ct);
        (DbManga source, DbMetadata sourceMetadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Source", ct: ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);

        List<DbMangaMetadataEntries> entries = await context.MangaMetadataEntries.AsNoTracking()
            .Where(e => e.MangaId == target.MangaId).ToListAsync(ct);

        Assert.Equal(2, entries.Count);
        Assert.Single(entries, e => e.MetadataId == targetMetadata.MetadataId && e.Chosen);
        Assert.Single(entries, e => e.MetadataId == sourceMetadata.MetadataId && !e.Chosen);
    }

    [Fact]
    public async Task Merge_KeepsSourceMetadataWhenRequested()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Target", ct: ct);
        (DbManga source, DbMetadata sourceMetadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Source", ct: ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, new(source.MangaId, KeepSourceMetadata: true, KeepSourceChapters: false), ct);

        Assert.IsType<Ok>(result.Result);

        DbMangaMetadataEntries chosen = await context.MangaMetadataEntries.AsNoTracking()
            .SingleAsync(e => e.MangaId == target.MangaId && e.Chosen, ct);
        Assert.Equal(sourceMetadata.MetadataId, chosen.MetadataId);
    }

    [Fact]
    public async Task Merge_UnionsDownloadLinksAndRenormalizesPriority()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        await TestDataBuilder.SeedMangaDownloadLink(context, target, matched: true, priority: 0, ct: ct);
        await TestDataBuilder.SeedMangaDownloadLink(context, source, matched: true, priority: 0, ct: ct);
        await TestDataBuilder.SeedMangaDownloadLink(context, source, matched: false, priority: 5, ct: ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);

        List<DbMangaDownloadLinks> links = await context.MangaDownloadLinks.AsNoTracking()
            .Where(l => l.MangaId == target.MangaId).ToListAsync(ct);

        Assert.Equal(3, links.Count);
        List<int> matchedPriorities = links.Where(l => l.Matched).Select(l => l.Priority).OrderBy(p => p).ToList();
        Assert.Equal([0, 1], matchedPriorities);
    }

    [Fact]
    public async Task Merge_DedupesDownloadLinksPresentOnBothManga_PrefersMatched()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        DbMangaDownloadLinks targetLink = await TestDataBuilder.SeedMangaDownloadLink(context, target, matched: false, priority: 3, ct: ct);
        DbMangaDownloadLinks sourceLink = new()
        {
            MangaId = source.MangaId,
            DownloadLinkId = targetLink.DownloadLinkId,
            Matched = true,
            Priority = 0,
            Manga = source,
            DownloadLink = targetLink.DownloadLink,
        };
        await context.AddAsync(sourceLink, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);

        DbMangaDownloadLinks surviving = await context.MangaDownloadLinks.AsNoTracking()
            .SingleAsync(l => l.MangaId == target.MangaId && l.DownloadLinkId == targetLink.DownloadLinkId, ct);
        Assert.True(surviving.Matched);
    }

    [Fact]
    public async Task Merge_KeepsTargetChaptersByDefault_DeletesSourceChapters()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        DbChapter targetChapter = await TestDataBuilder.SeedChapter(context, target, "1", ct);
        await TestDataBuilder.SeedChapter(context, source, "1", ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);

        List<DbChapter> chapters = await context.Chapters.AsNoTracking().Where(c => c.MangaId == target.MangaId).ToListAsync(ct);
        Assert.Single(chapters);
        Assert.Equal(targetChapter.ChapterId, chapters[0].ChapterId);
    }

    [Fact]
    public async Task Merge_RepointsSourceChaptersWhenKeepSourceChaptersRequested_DeletesTargetChapters()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        await TestDataBuilder.SeedChapter(context, target, "1", ct);
        DbChapter sourceChapter = await TestDataBuilder.SeedChapter(context, source, "1", ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, new(source.MangaId, KeepSourceMetadata: false, KeepSourceChapters: true), ct);

        Assert.IsType<Ok>(result.Result);

        List<DbChapter> chapters = await context.Chapters.AsNoTracking().Where(c => c.MangaId == target.MangaId).ToListAsync(ct);
        Assert.Single(chapters);
        Assert.Equal(sourceChapter.ChapterId, chapters[0].ChapterId);
    }

    [Fact]
    public async Task Merge_DeletesSourceManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);
        Assert.False(await context.Mangas.AsNoTracking().AnyAsync(m => m.MangaId == source.MangaId, ct));
        Assert.True(await context.Mangas.AsNoTracking().AnyAsync(m => m.MangaId == target.MangaId, ct));
    }

    [Fact]
    public async Task Merge_Returns404WhenTargetOrSourceDoesNotExist()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound, BadRequest<string>> missingSource = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), manga.MangaId, KeepTargetDefaults with { SourceMangaId = Guid.NewGuid() }, ct);
        Assert.IsType<NotFound>(missingSource.Result);

        Results<Ok, NotFound, BadRequest<string>> missingTarget = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), Guid.NewGuid(), KeepTargetDefaults with { SourceMangaId = manga.MangaId }, ct);
        Assert.IsType<NotFound>(missingTarget.Result);
    }

    [Fact]
    public async Task Merge_Returns400WhenMergingMangaIntoItself()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, CreatePublisher(), manga.MangaId, KeepTargetDefaults with { SourceMangaId = manga.MangaId }, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task Merge_PublishesMangaMergedAndMangaUpdatedEvents()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga target, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        (DbManga source, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Mock<IChannel> mockChannel = new();
        mockChannel.SetupGet(c => c.IsOpen).Returns(true);
        EventPublisher publisher = new(mockChannel.Object);

        Results<Ok, NotFound, BadRequest<string>> result = await PostMangaMergeEndpoint.Handle(
            context, publisher, target.MangaId, KeepTargetDefaults with { SourceMangaId = source.MangaId }, ct);

        Assert.IsType<Ok>(result.Result);

        // BasicPublishAsync is a generic method (constrained to RabbitMQ's internal EmptyBasicProperty type
        // argument), which Moq cannot Setup/Verify by expression - inspect the raw invocation list instead and
        // pull the routing key (2nd positional argument), which EventPublisher sets to the event type's name.
        List<string?> publishedRoutingKeys = mockChannel.Invocations
            .Where(i => i.Method.Name == nameof(IChannel.BasicPublishAsync))
            .Select(i => i.Arguments[1] as string)
            .ToList();

        Assert.Contains(nameof(MangaMergedEvent), publishedRoutingKeys);
        Assert.Contains(nameof(MangaUpdatedEvent), publishedRoutingKeys);
    }
}
