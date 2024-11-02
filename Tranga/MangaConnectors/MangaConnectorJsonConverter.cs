using System.Data;
using System.Diagnostics;
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
        string? connectorName = jo.Value<string>("name");
        if (connectorName is null)
            throw new ConstraintException("Name can not be null.");
        return connectorName switch
        {
            "MangaDex" => this._connectors.First(c => c is MangaDex),
            "Manganato" => this._connectors.First(c => c is Manganato),
            "MangaKatana" => this._connectors.First(c => c is MangaKatana),
            "Mangasee" => this._connectors.First(c => c is Mangasee),
            "Mangaworld" => this._connectors.First(c => c is Mangaworld),
            "Bato" => this._connectors.First(c => c is Bato),
            "Manga4Life" => this._connectors.First(c => c is MangaLife),
            "ManhuaPlus" => this._connectors.First(c => c is ManhuaPlus),
            "MangaHere" => this._connectors.First(c => c is MangaHere),
            "AsuraToon" => this._connectors.First(c => c is AsuraToon),
            _ => throw new UnreachableException($"Could not find Connector with name {connectorName}")
        };
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