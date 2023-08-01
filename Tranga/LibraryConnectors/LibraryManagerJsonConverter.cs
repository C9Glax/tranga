using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.LibraryConnectors;

public class LibraryManagerJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(LibraryConnector));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryConnector.LibraryType.Komga)
            return jo.ToObject<Komga>(serializer)!;

        if (jo["libraryType"]!.Value<Int64>() == (Int64)LibraryConnector.LibraryType.Kavita)
            return jo.ToObject<Kavita>(serializer)!;

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