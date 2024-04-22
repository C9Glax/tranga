using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class JobJsonConverter : JsonConverter
{
    private GlobalBase _clone;
    private MangaConnectorJsonConverter _mangaConnectorJsonConverter;

    internal JobJsonConverter(GlobalBase clone, MangaConnectorJsonConverter mangaConnectorJsonConverter)
    {
        this._clone = clone;
        this._mangaConnectorJsonConverter = mangaConnectorJsonConverter;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(Job));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        
        if(!jo.ContainsKey("jobType"))
            throw new Exception();

        return Enum.Parse<Job.JobType>(jo["jobType"]!.Value<byte>().ToString()) switch
        {
            Job.JobType.UpdateMetaDataJob => new UpdateMetadata(_clone, 
                jo.GetValue("manga")!.ToObject<Manga>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters = { this._mangaConnectorJsonConverter }
                })), 
                jo.GetValue("parentJobId")!.Value<string?>()),
            Job.JobType.DownloadChapterJob => new DownloadChapter(this._clone,
                jo.GetValue("mangaConnector")!.ToObject<MangaConnector>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters = { this._mangaConnectorJsonConverter }
                }))!,
                jo.GetValue("chapter")!.ToObject<Chapter>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters = { this._mangaConnectorJsonConverter }
                })),
                DateTime.UnixEpoch,
                jo.GetValue("parentJobId")!.Value<string?>()),
            Job.JobType.DownloadNewChaptersJob => new DownloadNewChapters(this._clone,
                jo.GetValue("manga")!.ToObject<Manga>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters = { this._mangaConnectorJsonConverter }
                })), 
                jo.GetValue("lastExecution") is {} le 
                    ? le.ToObject<DateTime>()
                    : DateTime.UnixEpoch,
                jo.GetValue("recurring")!.Value<bool>(),
                jo.GetValue("recurrenceTime")!.ToObject<TimeSpan?>(),
                jo.GetValue("parentJobId")!.Value<string?>()),
            _ => throw new Exception()
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