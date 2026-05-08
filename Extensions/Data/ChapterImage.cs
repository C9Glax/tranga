using Common.Helpers;

namespace Extensions.Data;

public sealed record ChapterImage(
    Guid ExtensionIdentifier,
    string chapterIdentifier,
    int order,
    TrangaImage image
    );