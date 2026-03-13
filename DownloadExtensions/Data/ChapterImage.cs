namespace DownloadExtensions.Data;

public sealed record ChapterImage<T>(string chapterIdentifier, int order, MemoryStream image) where T : IExtension<T>;