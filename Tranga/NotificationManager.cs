using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.NotificationManagers;

namespace Tranga;

public abstract class NotificationManager
{
    protected Logger? logger;
    public NotificationManagerType notificationManagerType { get; }

    protected NotificationManager(NotificationManagerType notificationManagerType, Logger? logger = null)
    {
        this.notificationManagerType = notificationManagerType;
        this.logger = logger;
    }
    
    public enum NotificationManagerType : byte { Gotify = 0, LunaSea = 1 }
    
    public abstract void SendNotification(string title, string notificationText);
    
    public class NotificationManagerJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(NotificationManager));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["notificationManagerType"]!.Value<byte>() == (byte)NotificationManagerType.Gotify)
                return jo.ToObject<Gotify>(serializer)!;

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
}