using Extensions.Extensions;

namespace Extensions;

/// <summary>Provides the download extensions that are compiled into Tranga.</summary>
public sealed class BuiltInDownloadExtensionProvider : IDownloadExtensionProvider
{
    public string Name => "Built-in";

    public Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<IDownloadExtension>>([new MangaDex()]);
}
