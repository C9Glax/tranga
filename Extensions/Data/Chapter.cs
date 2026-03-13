namespace Extensions.Data;

public sealed record Chapter<T>(
    string Number,
    string Url,
    IChapterIdentifier<T> Identifier,
    string? Volume = null,
    string? Title = null
) where T : IExtension<T>;

public interface IChapterIdentifier<T> where T : IExtension<T>;