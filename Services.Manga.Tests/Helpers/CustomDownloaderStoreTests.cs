using Common.Tests;
using Extensions;
using Extensions.Custom;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class CustomDownloaderStoreTests : TrangaTest
{
    private static DbCustomDownloader Row(SelectorDownloaderDefinition definition) => new()
    {
        CustomDownloaderId = Guid.NewGuid(),
        Name = definition.Name,
        DefinitionJson = SelectorDownloaderJson.Serialize(definition)
    };

    [Fact]
    public void ToDefinition_UsesTheRowsIdNotTheOneInTheJson()
    {
        DbCustomDownloader row = Row(SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.NewGuid() });

        SelectorDownloaderDefinition? definition = CustomDownloaderStore.ToDefinition(row);

        Assert.NotNull(definition);
        Assert.Equal(row.CustomDownloaderId, definition.Id);
    }

    [Fact]
    public void ToDefinition_ReturnsNullForUnreadableJson() =>
        Assert.Null(CustomDownloaderStore.ToDefinition(new DbCustomDownloader { CustomDownloaderId = Guid.NewGuid(), Name = "x", DefinitionJson = "{{{" }));

    [Fact]
    public async Task Provider_LoadsStoredSourcesAsExtensions()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbCustomDownloader good = Row(SelectorDownloaderJson.WeebCentralExample() with { Name = "Stored" });
        DbCustomDownloader invalid = Row(SelectorDownloaderJson.WeebCentralExample() with { Name = "Invalid", Search = new SearchSpec() });
        await context.CustomDownloaders.AddRangeAsync([good, invalid, new DbCustomDownloader { CustomDownloaderId = Guid.NewGuid(), Name = "Broken", DefinitionJson = "nope" }], ct);
        await context.SaveChangesAsync(ct);
        ServiceCollection services = new();
        // like in production every load gets its own scoped context
        services.AddScoped(_ => MangaContextFactory.Reopen(context));

        CustomDownloadExtensionProvider provider = CustomDownloaderStore.CreateProvider(services.BuildServiceProvider());
        IReadOnlyList<IDownloadExtension> extensions = await provider.LoadAsync(ct);

        IDownloadExtension extension = Assert.Single(extensions);
        Assert.Equal(good.CustomDownloaderId, extension.Identifier);
        Assert.Equal("Stored", extension.Name);
    }
}
