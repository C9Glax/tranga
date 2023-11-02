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

        if (jo.ContainsKey("jobType") && jo["jobType"]!.Value<byte>() == (byte)Job.JobType.UpdateMetaDataJob)
        {
            return new UpdateMetadata(this._clone,
                jo.GetValue("mangaConnector")!.ToObject<MangaConnector>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters =
                    {
                        this._mangaConnectorJsonConverter
                    }
                }))!,
                jo.GetValue("manga")!.ToObject<Manga>(),
                jo.GetValue("parentJobId")!.Value<string?>());
        }else if ((jo.ContainsKey("jobType") && jo["jobType"]!.Value<byte>() == (byte)Job.JobType.DownloadNewChaptersJob) || jo.ContainsKey("translatedLanguage"))//TODO change to jobType
        {
            return new DownloadNewChapters(this._clone,
                jo.GetValue("mangaConnector")!.ToObject<MangaConnector>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters =
                    {
                        this._mangaConnectorJsonConverter
                    }
                }))!,
                jo.GetValue("manga")!.ToObject<Manga>(),
                jo.GetValue("lastExecution")!.ToObject<DateTime>(),
                jo.GetValue("recurring")!.Value<bool>(),
                jo.GetValue("recurrenceTime")!.ToObject<TimeSpan?>(),
                jo.GetValue("parentJobId")!.Value<string?>());
        }else if ((jo.ContainsKey("jobType") && jo["jobType"]!.Value<byte>() == (byte)Job.JobType.DownloadChapterJob) || jo.ContainsKey("chapter"))//TODO change to jobType
        {
            return new DownloadChapter(this._clone,
                jo.GetValue("mangaConnector")!.ToObject<MangaConnector>(JsonSerializer.Create(new JsonSerializerSettings()
                {
                    Converters =
                    {
                        this._mangaConnectorJsonConverter
                    }
                }))!,
                jo.GetValue("chapter")!.ToObject<Chapter>(),
                DateTime.UnixEpoch,
                jo.GetValue("parentJobId")!.Value<string?>());
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