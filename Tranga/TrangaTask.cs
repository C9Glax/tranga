using System.Net;
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
[JsonDerivedType(typeof(MonitorPublicationTask), 2)]
[JsonDerivedType(typeof(UpdateLibrariesTask), 3)]
[JsonDerivedType(typeof(DownloadChapterTask), 4)]
public abstract class TrangaTask
{
    // ReSharper disable once CommentTypo ...Tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    [Newtonsoft.Json.JsonIgnore] public ExecutionState state { get; set; }
    public Task task { get; }
    public string taskId { get; init; }
    [Newtonsoft.Json.JsonIgnore] public TrangaTask? parentTask { get; set; }
    public string? parentTaskId { get; set; }
    [Newtonsoft.Json.JsonIgnore] protected HashSet<TrangaTask> childTasks { get; }
    public double progress => GetProgress();
    [Newtonsoft.Json.JsonIgnore]public DateTime executionStarted { get; private set; }
    [Newtonsoft.Json.JsonIgnore]public DateTime lastChange { get; private set; }
    [Newtonsoft.Json.JsonIgnore]public DateTime executionApproximatelyFinished => progress != 0 ? lastChange.Add(GetRemainingTime()) : DateTime.MaxValue;
    public TimeSpan executionApproximatelyRemaining => executionApproximatelyFinished.Subtract(DateTime.Now);
    [Newtonsoft.Json.JsonIgnore]public DateTime nextExecution => lastExecuted.Add(reoccurrence);

    public enum ExecutionState { Waiting, Enqueued, Running, Failed, Success }

    protected TrangaTask(Task task, TimeSpan reoccurrence, TrangaTask? parentTask = null)
    {
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.task = task;
        this.executionStarted = DateTime.UnixEpoch;
        this.lastChange = DateTime.MaxValue;
        this.taskId = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next()));
        this.childTasks = new();
        this.parentTask = parentTask;
        this.parentTaskId = parentTask?.taskId;
    }
    
    /// <summary>
    /// BL for concrete Tasks
    /// </summary>
    /// <param name="taskManager"></param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    protected abstract HttpStatusCode ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null);

    public abstract TrangaTask Clone();

    protected abstract double GetProgress();

    /// <summary>
    /// Execute the task
    /// </summary>
    /// <param name="taskManager">Should be the parent taskManager</param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    public void Execute(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Executing Task {this}");
        this.state = ExecutionState.Running;
        this.executionStarted = DateTime.Now;
        this.lastChange = DateTime.Now;
        HttpStatusCode statusCode = ExecuteTask(taskManager, logger, cancellationToken);
        while(childTasks.Any(ct => ct.state is ExecutionState.Enqueued or ExecutionState.Running))
            Thread.Sleep(1000);
        foreach(TrangaTask childTask in this.childTasks.ToArray())
            taskManager.DeleteTask(childTask);
        if ((int)statusCode >= 200 && (int)statusCode < 300)
        {
            this.lastExecuted = DateTime.Now;
            if (this is DownloadChapterTask)
                this.state = ExecutionState.Success;
            else
                this.state = ExecutionState.Waiting;
        }
        else
        {
            if (this is DownloadChapterTask && statusCode == HttpStatusCode.NotFound)
                this.state = ExecutionState.Success;
            else
                this.state = ExecutionState.Failed;
            this.lastExecuted = DateTime.MaxValue;
        }
        logger?.WriteLine(this.GetType().ToString(), $"Finished Executing Task {this}");
    }

    public void AddChildTask(TrangaTask childTask)
    {
        this.childTasks.Add(childTask);
    }

    public void RemoveChildTask(TrangaTask childTask)
    {
        this.childTasks.Remove(childTask);
    }

    private TimeSpan GetRemainingTime()
    {
        if(progress == 0 || lastChange == DateTime.MaxValue || executionStarted == DateTime.UnixEpoch)
            return TimeSpan.Zero;
        TimeSpan elapsed = lastChange.Subtract(executionStarted);
        return elapsed.Divide(progress).Subtract(elapsed);
    }

    public enum Task : byte
    {
        MonitorPublication = 2,
        UpdateLibraries = 3,
        DownloadChapter = 4,
        DownloadNewChapters = 2 //legacy
    }

    public override string ToString()
    {
        return $"{task}, {lastExecuted}, {reoccurrence}, {state}, {progress:P2}, {executionApproximatelyFinished}, {executionApproximatelyRemaining}";
    }
    
    public class TrangaTaskJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TrangaTask);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["task"]!.Value<Int64>() == (Int64)Task.MonitorPublication)
                return jo.ToObject<MonitorPublicationTask>(serializer)!;

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