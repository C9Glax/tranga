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
    public static SelectorDownloaderDefinition WeebCentralExample()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Extensions.Custom.Examples.weebcentral.json")
                              ?? throw new InvalidOperationException("The WeebCentral example is not embedded.");
        return JsonSerializer.Deserialize<SelectorDownloaderDefinition>(stream, Options)!;
    }
}
