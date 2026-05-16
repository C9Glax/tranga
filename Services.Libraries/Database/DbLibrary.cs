namespace Services.Libraries.Database;

public sealed record DbLibrary(LibraryType LibraryType, string BaseUrl, string ApiKey)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public LibraryType LibraryType { get; init; } = LibraryType;
    public string BaseUrl { get; init; } = BaseUrl;
    public string ApiKey { get; init; } = ApiKey;
}

public enum LibraryType
{
    Komga
}