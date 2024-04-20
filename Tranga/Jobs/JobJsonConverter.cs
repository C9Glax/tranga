using JobQueue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class JobJsonConverter : JsonConverter
{
    private GlobalBase _clone;
    private MangaConnectorJsonConverter _mangaConnectorJsonConverter;
    private JobQueue<MangaConnector> queue;
    private ILogger? logger;

    internal JobJsonConverter(GlobalBase clone, MangaConnectorJsonConverter mangaConnectorJsonConverter, JobQueue<MangaConnector> queue, ILogger? logger = null)
    {
        this._clone = clone;
        this._mangaConnectorJsonConverter = mangaConnectorJsonConverter;
        this.queue = queue;
        this.logger = logger;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(Job));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        Job.JobType? jobType = (Job.JobType?)jo["jobType"]?.Value<byte>();
        MangaConnector? mangaConnector = jo.GetValue("mangaConnector")?.ToObject<MangaConnector>(JsonSerializer.Create(
            new JsonSerializerSettings()
            {
                Converters =
                {
                    this._mangaConnectorJsonConverter
                }
            }));
        if(mangaConnector is null)
            throw new JsonException("Could not deserialize this type");
        
        switch (jobType)
        {
            case Job.JobType.UpdateMetaDataJob:
                return new UpdateMetadata(_clone,
                    queue,
                    mangaConnector,
                    jo.GetValue("manga")!.ToObject<Manga>(),
                    jo.GetValue("jobId")!.Value<string>(),
                    jo.GetValue("parentJobId")!.Value<string?>(),
                    logger);    
            case Job.JobType.DownloadChapterJob:
                return new DownloadChapter(_clone,
                    queue,
                    mangaConnector,
                    jo.GetValue("chapter")!.ToObject<Chapter>(),
                    jo.GetValue("steps")!.Value<int>(),
                    jo.GetValue("jobId")!.Value<string>(),
                    jo.GetValue("parentJobId")!.Value<string?>(),
                    logger);
                break;
            case Job.JobType.DownloadNewChaptersJob:
                return new DownloadNewChapter(_clone,
                    queue,
                    mangaConnector,
                    jo.GetValue("manga")!.ToObject<Manga>(),
                    jo.GetValue("interval")!.ToObject<TimeSpan>(),
                    jo.GetValue("steps")!.Value<int>(),
                    jo.GetValue("jobId")!.Value<string>(),
                    jo.GetValue("parentJobId")!.Value<string?>(),
                    logger);
                break;
            default:
                throw new JsonException("Could not deserialize this type");
        }
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