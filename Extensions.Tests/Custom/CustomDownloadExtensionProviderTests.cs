using Extensions.Custom;
using Extensions.Extensions;

namespace Extensions.Tests.Custom;

public sealed class CustomDownloadExtensionProviderTests : Common.Tests.TrangaTest
{
    private static readonly SelectorDownloaderOptions Options = new(AllowPrivateHosts: false, FlareSolverrUrl: null);

    private static SelectorDownloaderDefinition Definition(string name = "Custom") =>
        SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.NewGuid(), Name = name };

    [Fact]
    public async Task ProvidesAnExtensionPerValidDefinitionAndSkipsInvalidOnes()
    {
        SelectorDownloaderDefinition good = Definition("Good");
        SelectorDownloaderDefinition broken = Definition("Broken") with { Search = new SearchSpec() };
        CustomDownloadExtensionProvider provider = new(_ => Task.FromResult<IReadOnlyList<SelectorDownloaderDefinition>>([good, broken]), Options);

        IReadOnlyList<IDownloadExtension> extensions = await provider.LoadAsync(ct);

        IDownloadExtension extension = Assert.Single(extensions);
        Assert.Equal(good.Id, extension.Identifier);
        Assert.Equal("Good", extension.Name);
        Assert.IsType<SelectorDownloadExtension>(extension);
    }

    [Fact]
    public async Task ReloadsFromTheLoaderEveryTime()
    {
        List<SelectorDownloaderDefinition> stored = [Definition("One")];
        CustomDownloadExtensionProvider provider = new(_ => Task.FromResult<IReadOnlyList<SelectorDownloaderDefinition>>([.. stored]), Options);

        Assert.Single(await provider.LoadAsync(ct));
        stored.Add(Definition("Two"));
        Assert.Equal(2, (await provider.LoadAsync(ct)).Count);
        stored.Clear();
        Assert.Empty(await provider.LoadAsync(ct));
    }

    [Fact]
    public async Task RegistryPicksUpCustomSourcesOnRefreshAndForgetsDeletedOnes()
    {
        List<SelectorDownloaderDefinition> stored = [Definition("Registered")];
        DownloadExtensionsCollection.RegisterProvider(
            new CustomDownloadExtensionProvider(_ => Task.FromResult<IReadOnlyList<SelectorDownloaderDefinition>>([.. stored]), Options));
        Guid id = stored[0].Id;

        await DownloadExtensionsCollection.RefreshAsync(ct);
        Assert.IsType<SelectorDownloadExtension>(DownloadExtensionsCollection.GetExtension(id));
        Assert.Contains(DownloadExtensionsCollection.Extensions, e => e is MangaDex); // built-ins stay

        stored.Clear();
        await DownloadExtensionsCollection.RefreshAsync(ct);
        Assert.Null(DownloadExtensionsCollection.GetExtension(id));
    }
}
