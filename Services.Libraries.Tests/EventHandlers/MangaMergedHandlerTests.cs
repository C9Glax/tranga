using System.Reflection;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.EventHandlers;
using Services.Libraries.Tests.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Tests.EventHandlers;

public sealed class MangaMergedHandlerTests : Common.Tests.TrangaTest
{
    /// <summary>
    /// <c>HandleMessage</c> is protected, so it is invoked via reflection rather than requiring a public seam in
    /// production code (mirrors the reflection approach used in <see cref="MangaUpdatedHandlerTests"/>).
    /// </summary>
    private static async Task<bool> InvokeHandleMessage(MangaMergedHandler handler, MangaMergedEvent mangaMergedEvent)
    {
        MethodInfo? method = typeof(MangaMergedHandler).GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        object? result = method.Invoke(handler, [mangaMergedEvent]);
        Assert.NotNull(result);
        return await (Task<bool>)result;
    }

    private static ServiceProvider BuildServiceProvider(LibrariesContext librariesContext)
    {
        string librariesDbPath = librariesContext.Database.GetDbConnection().DataSource;

        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<LibrariesContext>(o => o.UseSqlite($"Data Source={librariesDbPath}"));
        return services.BuildServiceProvider();
    }

    private static MangaMergedHandler CreateHandler(IServiceProvider serviceProvider)
    {
        Mock<IChannel> mockChannel = new();
        return new MangaMergedHandler(mockChannel.Object, serviceProvider);
    }

    [Fact]
    public async Task HandleMessage_RepointsSourceMappingToTarget_WhenTargetHasNoMapping()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();

        Guid sourceMangaId = Guid.NewGuid();
        Guid targetMangaId = Guid.NewGuid();
        DbLibraryService dbLibrary = new(LibraryServiceType.Komga, "MyLibrary", "http://localhost:1/", "some-api-key") { TrangaLibraryId = "komga-library-id" };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, sourceMangaId, "series-1"), ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext);
        MangaMergedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaMergedEvent(sourceMangaId, targetMangaId));

        Assert.True(result);
        DbMangaIdMapping mapping = await librariesContext.MangaMappings.AsNoTracking()
            .SingleAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId, ct);
        Assert.Equal(targetMangaId, mapping.MangaId);
        Assert.Equal("series-1", mapping.SeriesId);
    }

    [Fact]
    public async Task HandleMessage_DeletesSourceMapping_WhenTargetAlreadyMapped()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();

        Guid sourceMangaId = Guid.NewGuid();
        Guid targetMangaId = Guid.NewGuid();
        DbLibraryService dbLibrary = new(LibraryServiceType.Komga, "MyLibrary", "http://localhost:1/", "some-api-key") { TrangaLibraryId = "komga-library-id" };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, sourceMangaId, "series-1"), ct);
        await librariesContext.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, targetMangaId, "series-2"), ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext);
        MangaMergedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaMergedEvent(sourceMangaId, targetMangaId));

        Assert.True(result);
        List<DbMangaIdMapping> mappings = await librariesContext.MangaMappings.AsNoTracking()
            .Where(m => m.LibraryServiceId == dbLibrary.LibraryServiceId).ToListAsync(ct);
        DbMangaIdMapping mapping = Assert.Single(mappings);
        Assert.Equal(targetMangaId, mapping.MangaId);
        Assert.Equal("series-2", mapping.SeriesId);
    }

    [Fact]
    public async Task HandleMessage_NoOp_WhenSourceHasNoMappings()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext);
        MangaMergedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaMergedEvent(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result);
    }
}
