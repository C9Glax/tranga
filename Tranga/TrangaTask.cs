using System.Text.Json.Serialization;
using Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.TrangaTasks;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace Tranga;

/// <summary>
/// Stores information on Task, when implementing new Tasks also update the serializer
/// </summary>
[JsonDerivedType(typeof(DownloadNewChaptersTask), 2)]
[JsonDerivedType(typeof(UpdateLibrariesTask), 3)]
[JsonDerivedType(typeof(DownloadChapterTask), 4)]
public abstract class TrangaTask
{
    // ReSharper disable once CommentTypo ...Tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public Task task { get; }
    [Newtonsoft.Json.JsonIgnore]public ExecutionState state { get; set; }
    [Newtonsoft.Json.JsonIgnore]public float progress { get; protected set; }
    [Newtonsoft.Json.JsonIgnore]public DateTime nextExecution => lastExecuted.Add(reoccurrence);
    [Newtonsoft.Json.JsonIgnore]public DateTime executionStarted { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    public DateTime executionApproximatelyFinished => this.progress != 0
        ? this.executionStarted.Add(DateTime.Now.Subtract(this.executionStarted) / this.progress)
        : DateTime.MaxValue;
    
    [Newtonsoft.Json.JsonIgnore]
    public TimeSpan executionApproximatelyRemaining => this.executionApproximatelyFinished.Subtract(DateTime.Now);
    
    [Newtonsoft.Json.JsonIgnore]public DateTime lastChange { get; protected set; }

    public enum ExecutionState
    {
        Waiting,
        Enqueued,
        Running
    };

    protected TrangaTask(Task task, TimeSpan reoccurrence)
    {
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.task = task;
        this.progress = 0f;
        this.executionStarted = DateTime.Now;
        this.lastChange = DateTime.Now;
    }

    public float IncrementProgress(float amount)
    {
        this.progress += amount;
        this.lastChange = DateTime.Now;
        return this.progress;
    }

    public float DecrementProgress(float amount)
    {
        this.progress -= amount;
        this.lastChange = DateTime.Now;
        return this.progress;
    }
    
    /// <summary>
    /// BL for concrete Tasks
    /// </summary>
    /// <param name="taskManager"></param>
    /// <param name="logger"></param>
    protected abstract void ExecuteTask(TaskManager taskManager, Logger? logger);

    /// <summary>
    /// Execute the task
    /// </summary>
    /// <param name="taskManager">Should be the parent taskManager</param>
    /// <param name="logger"></param>
    public void Execute(TaskManager taskManager, Logger? logger)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Executing Task {this}");
        this.state = ExecutionState.Running;
        this.executionStarted = DateTime.Now;
        ExecuteTask(taskManager, logger);
        this.lastExecuted = DateTime.Now;
        this.state = ExecutionState.Waiting;
        logger?.WriteLine(this.GetType().ToString(), $"Finished Executing Task {this}");
    }

    /// <returns>True if elapsed time since last execution is greater than set interval</returns>
    public bool ShouldExecute()
    {
        return nextExecution < DateTime.Now && state is ExecutionState.Waiting;
    }

    public enum Task : byte
    {
        DownloadNewChapters = 2,
        UpdateLibraries = 3,
        DownloadChapter = 4
    }

    public override string ToString()
    {
        return $"{task}, {lastExecuted}, {reoccurrence}, {state}, {progress:P2}, {executionApproximatelyFinished}, {executionApproximatelyRemaining}";
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

            if (jo["task"]!.Value<Int64>() == (Int64)Task.UpdateLibraries)
                return jo.ToObject<UpdateLibrariesTask>(serializer)!;
            
            if (jo["task"]!.Value<Int64>() == (Int64)Task.DownloadChapter)
                return jo.ToObject<DownloadChapterTask>(serializer)!;

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