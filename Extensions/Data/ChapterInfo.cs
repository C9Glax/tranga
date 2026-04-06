namespace Extensions.Data;

public sealed record ChapterInfo(
    Guid ExtensionIdentifier,
    string Number,
    string Url,
    string Identifier,
    string? Volume = null,
    string? Title = null
);