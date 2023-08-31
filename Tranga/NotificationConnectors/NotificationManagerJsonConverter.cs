using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tranga.NotificationConnectors;

public class NotificationManagerJsonConverter : JsonConverter
{
    private GlobalBase _clone;

    public NotificationManagerJsonConverter(GlobalBase clone)
    {
        this._clone = clone;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(NotificationConnector));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["notificationManagerType"]!.Value<byte>() == (byte)NotificationConnector.NotificationManagerType.Gotify)
            return new Gotify(this._clone, jo.GetValue("endpoint")!.Value<string>()!, jo.GetValue("appToken")!.Value<string>()!);
        else if (jo["notificationManagerType"]!.Value<byte>() ==
                 (byte)NotificationConnector.NotificationManagerType.LunaSea)
            return new LunaSea(this._clone, jo.GetValue("id")!.Value<string>()!);

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