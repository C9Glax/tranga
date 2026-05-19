using Extensions.Data;

namespace Services.Libraries.Database;

public sealed record DbLibraryService(LibraryServiceType LibraryServiceType, string Name, string BaseUrl, string ApiKey)
{
    public Guid LibraryServiceId { get; init; } = Guid.CreateVersion7();
    public LibraryServiceType LibraryServiceType { get; init; } = LibraryServiceType;
    public string Name { get; init; } = Name;
    public string BaseUrl { get; init; } = BaseUrl;
    public string ApiKey { get; init; } = ApiKey;
    public string TrangaLibraryId { get; internal set; }
}

public enum LibraryServiceType
{
    Komga
}