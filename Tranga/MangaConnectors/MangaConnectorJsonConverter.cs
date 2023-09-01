using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.MangaConnectors;

public class MangaConnectorJsonConverter : JsonConverter
{
    private GlobalBase _clone;
    private HashSet<MangaConnector> connectors;

    internal MangaConnectorJsonConverter(GlobalBase clone, HashSet<MangaConnector> connectors)
    {
        this._clone = clone;
        this.connectors = connectors;
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
                return this.connectors.First(c => c is MangaDex);
            case "Manganato":
                return this.connectors.First(c => c is Manganato);
            case "MangaKatana":
                return this.connectors.First(c => c is MangaKatana);
            case "Mangasee":
                return this.connectors.First(c => c is Mangasee);
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