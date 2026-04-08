namespace Services.Manga.Entities.DownloadExtensions;

public sealed record DownloadExtensionsList
{
    public IDownloadExtension[] Extensions { get; init; } =
    [
        new MangaDex()
    ];
}