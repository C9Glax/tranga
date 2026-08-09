using System.Reflection;
using Komga.Client.Api;
using Services.Libraries.Database;
using KomgaExtension = Extensions.Extensions.Komga;

namespace Services.Libraries.Tests.Helpers;

public sealed class DbLibraryToLibraryExtensionTests : Common.Tests.TrangaTest
{
    /// <summary>
    /// <c>DbLibraryToLibraryExtension.ToExtension</c> is internal, so it is invoked via reflection
    /// rather than requiring an InternalsVisibleTo seam in production code.
    /// </summary>
    private static KomgaExtension? InvokeToExtension(DbLibraryService libraryService)
    {
        Type? extensionType = Type.GetType("Services.Libraries.Helpers.DbLibraryToLibraryExtension, Services.Libraries");
        Assert.NotNull(extensionType);
        MethodInfo? method = extensionType.GetMethod("ToExtension", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return (KomgaExtension?)method.Invoke(null, [libraryService]);
    }

    private static string GetKomgaBasePath(KomgaExtension komga)
    {
        FieldInfo? field = typeof(KomgaExtension).GetField("_librariesApi", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        object? value = field.GetValue(komga);
        Assert.IsType<LibrariesApi>(value);
        return ((LibrariesApi)value).GetBasePath();
    }

    private static string GetKomgaApiKeyHeader(KomgaExtension komga)
    {
        FieldInfo? field = typeof(KomgaExtension).GetField("_komgaRequestClient", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        object? value = field.GetValue(komga);
        Assert.NotNull(value);
        PropertyInfo? headersProperty = value.GetType().GetProperty("DefaultRequestHeaders");
        Assert.NotNull(headersProperty);
        object? headers = headersProperty.GetValue(value);
        Assert.NotNull(headers);
        MethodInfo? getValuesMethod = headers.GetType().GetMethod("GetValues");
        Assert.NotNull(getValuesMethod);
        object? result = getValuesMethod.Invoke(headers, ["X-API-Key"]);
        Assert.NotNull(result);
        return Assert.Single((IEnumerable<string>)result);
    }

    [Fact]
    public void DbLibraryToLibraryExtension_ConvertsToKomgaCorrectly()
    {
        DbLibraryService libraryService = new(LibraryServiceType.Komga, "MyLibrary", "http://localhost:8080/", "some-api-key");

        KomgaExtension? extension = InvokeToExtension(libraryService);

        Assert.NotNull(extension);
        Assert.Equal("http://localhost:8080", GetKomgaBasePath(extension));
        Assert.Equal("some-api-key", GetKomgaApiKeyHeader(extension));
    }

    [Fact]
    public void DbLibraryToLibraryExtension_ReturnsNullForUnsupportedTypes()
    {
        DbLibraryService libraryService = new((LibraryServiceType)(-1), "MyLibrary", "http://localhost:8080/", "some-api-key");

        KomgaExtension? extension = InvokeToExtension(libraryService);

        Assert.Null(extension);
    }
}
