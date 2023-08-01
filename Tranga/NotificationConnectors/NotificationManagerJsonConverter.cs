using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.NotificationConnectors;

public class NotificationManagerJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(NotificationConnector));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["notificationManagerType"]!.Value<byte>() == (byte)NotificationConnector.NotificationManagerType.Gotify)
            return jo.ToObject<Gotify>(serializer)!;
        else if (jo["notificationManagerType"]!.Value<byte>() == (byte)NotificationConnector.NotificationManagerType.LunaSea)
            return jo.ToObject<LunaSea>(serializer)!;

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