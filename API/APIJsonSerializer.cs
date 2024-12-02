using System.Reflection;
using System.Text.Json;
using API.Schema;
using API.Schema.Jobs;
using API.Schema.LibraryConnectors;
using API.Schema.NotificationConnectors;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace API;

internal class ApiJsonSerializer : System.Text.Json.Serialization.JsonConverter<APISerializable>
{
    
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Job);

    public override APISerializable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<char> dest  = stackalloc char[1024];
        string json = "";
        while (reader.Read())
        {
            reader.CopyString(dest);
            json += dest.ToString();
        }
        JsonReader jr = new JsonTextReader(new StringReader(json));
        return new JobJsonDeserializer().ReadJson(jr, typeToConvert, null, JsonSerializer.Create(new JsonSerializerSettings())) as Job;
    }

    public override void Write(Utf8JsonWriter writer, APISerializable value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (PropertyInfo info in value.GetType().GetProperties())
        {
            if(info.PropertyType == typeof(string))
                writer.WriteString(LowerCamelCase(info.Name), (string)info.GetValue(value)!);
            else if(info.PropertyType == typeof(bool))
                writer.WriteBoolean(LowerCamelCase(info.Name), (bool)info.GetValue(value)!);
            else if(info.PropertyType == typeof(int))
                writer.WriteNumber(LowerCamelCase(info.Name), (int)info.GetValue(value)!);
            else if(info.PropertyType == typeof(ulong))
                writer.WriteNumber(LowerCamelCase(info.Name), (ulong)info.GetValue(value)!);
            else if(info.PropertyType == typeof(JobType))
                writer.WriteString(LowerCamelCase(info.Name), Enum.GetName((JobType)info.GetValue(value)!));
            else if(info.PropertyType == typeof(JobState))
                writer.WriteString(LowerCamelCase(info.Name), Enum.GetName((JobState)info.GetValue(value)!));
            else if(info.PropertyType == typeof(NotificationConnectorType))
                writer.WriteString(LowerCamelCase(info.Name), Enum.GetName((NotificationConnectorType)info.GetValue(value)!));
            else if(info.PropertyType == typeof(LibraryType))
                writer.WriteString(LowerCamelCase(info.Name), Enum.GetName((LibraryType)info.GetValue(value)!));
            else if(info.PropertyType == typeof(DateTime))
                writer.WriteString(LowerCamelCase(info.Name), ((DateTime)info.GetValue(value)!).ToUniversalTime().ToString("u").Replace(' ','T'));
        }
        writer.WriteEndObject();
    }

    private static string LowerCamelCase(string s)
    {
        return char.ToLowerInvariant(s[0]) + s.Substring(1);
    }
}