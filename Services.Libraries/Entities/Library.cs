using Services.Libraries.Database;

namespace Services.Libraries.Entities;

public sealed record Library(LibraryServiceType LibraryServiceType, Guid Id, string BaseUrl)
{
    public LibraryServiceType LibraryServiceType { get; init; } = LibraryServiceType;
    public Guid Id { get; init; } = Id;
    public string BaseUrl { get; init; } = BaseUrl;
}