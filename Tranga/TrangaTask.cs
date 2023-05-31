using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.TrangaTasks;

namespace Tranga;

/// <summary>
/// Stores information on Task
/// </summary>
public abstract class TrangaTask
{
    // ReSharper disable once CommentTypo ...Tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string? connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string? language { get; }
    [JsonIgnore]public ExecutionState state { get; set; }

    public enum ExecutionState
    {
        Waiting,
        Enqueued,
        Running
    };

    protected TrangaTask(Task task, string? connectorName, Publication? publication, TimeSpan reoccurrence, string? language = null)
    {
        this.publication = publication;
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.connectorName = connectorName;
        this.task = task;
        this.language = language;
    }

    /// <summary>
    /// Set state to running
    /// </summary>
    /// <param name="taskManager"></param>
    public abstract void Execute(TaskManager taskManager);

    /// <returns>True if elapsed time since last execution is greater than set interval</returns>
    public bool ShouldExecute()
    {
        return DateTime.Now.Subtract(this.lastExecuted) > reoccurrence && state is ExecutionState.Waiting;
    }

    public enum Task
    {
        DownloadNewChapters,
        UpdateKomgaLibrary
    }

    public override string ToString()
    {
        return $"{task}, {lastExecuted}, {reoccurrence}, {state} {(connectorName is not null ? $", {connectorName}" : "" )} {(publication is not null ? $", {publication?.sortName}": "")}";
    }
    
    public class TrangaTaskJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TrangaTask));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["task"]!.Value<Int64>() == (Int64)Task.DownloadNewChapters)
                return jo.ToObject<DownloadNewChaptersTask>(serializer)!;

            if (jo["task"]!.Value<Int64>() == (Int64)Task.UpdateKomgaLibrary)
                return jo.ToObject<UpdateKomgaLibraryTask>(serializer)!;

            throw new Exception();
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new Exception("Dont call this");
        }
    }
}