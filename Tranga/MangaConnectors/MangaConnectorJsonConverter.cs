using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.MangaConnectors;

public class MangaConnectorJsonConverter : JsonConverter
{
    private GlobalBase _clone;
    private readonly HashSet<MangaConnector> _connectors;

    internal MangaConnectorJsonConverter(GlobalBase clone, HashSet<MangaConnector> connectors)
    {
        this._clone = clone;
        this._connectors = connectors;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(MangaConnector));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        switch (jo.GetValue("name")!.Value<string>()!)
        {
            case "MangaDex":
                return this._connectors.First(c => c is MangaDex);
            case "Manganato":
                return this._connectors.First(c => c is Manganato);
            case "MangaKatana":
                return this._connectors.First(c => c is MangaKatana);
            case "Mangasee":
                return this._connectors.First(c => c is Mangasee);
            case "Mangaworld":
                return this._connectors.First(c => c is Mangaworld);
            case "Bato":
                return this._connectors.First(c => c is Bato);
            case "Manga4Life":
                return this._connectors.First(c => c is MangaLife);
        }

        throw new Exception();
    }

    public override bool CanWrite => false;

    /// <summary>
    /// Don't call this
    /// </summary>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new Exception("Dont call this");
    }
}