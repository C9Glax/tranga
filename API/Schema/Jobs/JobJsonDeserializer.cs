using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace API.Schema.Jobs;

public class JobJsonDeserializer : JsonConverter<Job>
{
    public override bool CanWrite { get; } = false;

    public override void WriteJson(JsonWriter writer, Job? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override Job? ReadJson(JsonReader reader, Type objectType, Job? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject j = JObject.Load(reader);
        JobType? type = Enum.Parse<JobType>(j.GetValue("jobType")!.Value<string>()!);
        return type switch
        {
            JobType.DownloadSingleChapterJob => j.ToObject<DownloadSingleChapterJob>(),
            JobType.DownloadNewChaptersJob => j.ToObject<DownloadNewChaptersJob>(), 
            JobType.UpdateMetaDataJob => j.ToObject<UpdateMetadataJob>(),
            JobType.MoveFileOrFolderJob => j.ToObject<MoveFileOrFolderJob>(),
            _ => null
        };
    }
}