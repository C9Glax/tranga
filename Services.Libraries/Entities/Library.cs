using Services.Libraries.Database;

namespace Services.Libraries.Entities;

public sealed record Library(LibraryType LibraryType, Guid Id, string BaseUrl)
{
    public LibraryType LibraryType { get; init; } = LibraryType;
    public Guid Id { get; init; } = Id;
    public string BaseUrl { get; init; } = BaseUrl;
}