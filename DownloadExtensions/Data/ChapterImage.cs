namespace DownloadExtensions.Data;

public sealed record ChapterImage(
    Guid ExtensionIdentifier,
    string chapterIdentifier,
    int order,
    MemoryStream image
    );