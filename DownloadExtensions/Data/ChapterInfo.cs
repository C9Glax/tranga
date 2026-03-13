namespace DownloadExtensions.Data;

public sealed record ChapterInfo<T>(
    string Number,
    string Url,
    string Identifier,
    string? Volume = null,
    string? Title = null
) where T : IDownloadExtension<T>;