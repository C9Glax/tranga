using Services.Libraries.Database;

namespace Services.Libraries.Helpers;

internal static class DbLibraryToLibraryExtension
{
    public static Extensions.Extensions.Komga? ToExtension(this DbLibraryService libraryService)
    {
        return libraryService.LibraryServiceType != LibraryServiceType.Komga ? null : new Extensions.Extensions.Komga(libraryService.BaseUrl);
    }
}