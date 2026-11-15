using Services.Libraries.Database;

namespace Services.Libraries.Helpers;

internal static class DbLibraryToLibraryExtension
{
    public static Extensions.Extensions.Komga? ToExtension(this DbLibraryService libraryService)
    {
        return libraryService.LibraryServiceType != LibraryServiceType.Komga ? null : new Extensions.Extensions.Komga(libraryService.BaseUrl, libraryService.ApiKey);
    }

    /// <summary>
    /// Service-agnostic client for any supported library type, or null for unsupported types.
    /// </summary>
    public static ILibraryClient? ToClient(this DbLibraryService libraryService)
    {
        return libraryService.LibraryServiceType switch
        {
            LibraryServiceType.Komga => new KomgaLibraryClient(new Extensions.Extensions.Komga(libraryService.BaseUrl, libraryService.ApiKey)),
            LibraryServiceType.Kavita => new KavitaLibraryClient(new Extensions.Extensions.Kavita(libraryService.BaseUrl, libraryService.ApiKey)),
            _ => null
        };
    }
}
