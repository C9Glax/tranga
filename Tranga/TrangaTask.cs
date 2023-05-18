namespace Tranga;

public class TrangaTask
{
    private readonly Action _taskAction;
    private Task? _task;
    public bool lastExecutedSuccessfully => _task is not null && _task.IsCompleted;
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; private set; }

    public TrangaTask(Action taskAction, TimeSpan reoccurrence)
    {
        this._taskAction = taskAction;
        this.reoccurrence = reoccurrence;
    }

    public void Abort()
    {
        if(_task is not null && !_task.IsCompleted)
            _task.Dispose();
    }

    public void Execute()
    {
        lastExecuted = DateTime.Now;
        _task = new (_taskAction);
        _task.Start();
    }

    public static TrangaTask CreateDownloadChapterTask(Connector connector, Chapter chapter, TimeSpan reoccurrence)
    {
        void TaskAction()
        {
            connector.DownloadChapter(chapter);
        }
        return new TrangaTask(TaskAction, reoccurrence);
    }

    
    public static TrangaTask CreateUpdateChaptersTask(Connector connector, Publication publication, TimeSpan reoccurrence)
    {
        throw new NotImplementedException();
    }

    public static TrangaTask CreateUpdatePublicationsTask(Connector connector, TimeSpan reoccurrence)
    {
        throw new NotImplementedException();
    }
}