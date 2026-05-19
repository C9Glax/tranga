namespace Services.Libraries.Database;

public sealed record DbLibrary(LibraryType LibraryType, string Name, string BaseUrl, string ApiKey)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public LibraryType LibraryType { get; init; } = LibraryType;
    public string Name { get; init; } = Name;
    public string BaseUrl { get; init; } = BaseUrl;
    public string ApiKey { get; init; } = ApiKey;
    
    
    #region Navigations

    internal ICollection<DbServiceDirectoryMapping>? ServiceDirectoryMappings { get; init; }

    #endregion
}

public enum LibraryType
{
    Komga
}