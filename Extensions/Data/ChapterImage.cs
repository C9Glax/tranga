namespace Extensions.Data;

public sealed record ChapterImage<T>(IChapterIdentifier<T> chapterIdentifier, int order, MemoryStream image) where T : IExtension<T>;