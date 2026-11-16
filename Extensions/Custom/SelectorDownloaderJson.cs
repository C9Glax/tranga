using System.Reflection;
using System.Text.Json;

namespace Extensions.Custom;

/// <summary>JSON (de)serialization of <see cref="SelectorDownloaderDefinition"/>s, shared by storage, import/export and tests.</summary>
public static class SelectorDownloaderJson
{
    /// <summary>camelCase, case-insensitive, same as ASP.NET's defaults.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>Serializes a definition for storage.</summary>
    public static string Serialize(SelectorDownloaderDefinition definition) => JsonSerializer.Serialize(definition, Options);

    /// <summary>Parses a stored definition. Null when the JSON is not a definition.</summary>
    public static SelectorDownloaderDefinition? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SelectorDownloaderDefinition>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// A ready-made definition for WeebCentral, mirroring the keiyoushi Tachiyomi extension. Used as a worked example
    /// and as a test fixture. Its <see cref="SelectorDownloaderDefinition.Id"/> is empty and must be replaced before use.
    /// </summary>
    public static SelectorDownloaderDefinition WeebCentralExample() => Example("weebcentral");

    /// <summary>
    /// A ready-made definition for MangaTown, mirroring the keiyoushi Tachiyomi extension. Unlike WeebCentral it shows
    /// one image per HTML page (see <see cref="ImagesSpec.PageSelector"/>) and takes the chapter number from the URL.
    /// Its <see cref="SelectorDownloaderDefinition.Id"/> is empty and must be replaced before use.
    /// </summary>
    public static SelectorDownloaderDefinition MangaTownExample() => Example("mangatown");

    /// <summary>
    /// A ready-made definition for Manga.uno, mirroring the keiyoushi Tachiyomi extension. Unlike the other two the
    /// website has no HTML to read: its pages are built by JavaScript from a JSON API, so the definition reads that API
    /// (<see cref="SourceFormat.Json"/>) and builds the links from ids. Its <see cref="SelectorDownloaderDefinition.Id"/>
    /// is empty and must be replaced before use.
    /// </summary>
    public static SelectorDownloaderDefinition MangaUnoExample() => Example("mangauno");

    /// <summary>All embedded examples, in the order they are offered to users.</summary>
    public static IReadOnlyList<SelectorDownloaderDefinition> Examples() => [WeebCentralExample(), MangaTownExample(), MangaUnoExample()];

    private static SelectorDownloaderDefinition Example(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Extensions.Custom.Examples.{name}.json")
                              ?? throw new InvalidOperationException($"The {name} example is not embedded.");
        return JsonSerializer.Deserialize<SelectorDownloaderDefinition>(stream, Options)!;
    }
}
