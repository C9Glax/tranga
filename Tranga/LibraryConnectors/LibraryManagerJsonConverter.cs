using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.LibraryConnectors;

public class LibraryManagerJsonConverter : JsonConverter
{
    private readonly GlobalBase _clone;

    internal LibraryManagerJsonConverter(GlobalBase clone)
    {
        this._clone = clone;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(LibraryConnector));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["libraryType"]!.Value<byte>() == (byte)LibraryConnector.LibraryType.Komga)
            return new Komga(this._clone, 
                jo.GetValue("baseUrl")!.Value<string>()!,
                jo.GetValue("auth")!.Value<string>()!);

        if (jo["libraryType"]!.Value<byte>() == (byte)LibraryConnector.LibraryType.Kavita)
            return new Kavita(this._clone,
                jo.GetValue("baseUrl")!.Value<string>()!,
                jo.GetValue("auth")!.Value<string>()!);

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