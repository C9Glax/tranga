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

    public static TrangaTask CreateDownloadChapterTask(Connector connector, Publication publication, Chapter chapter, TimeSpan reoccurrence)
    {
        void TaskAction()
        {
            connector.DownloadChapter(publication, chapter);
        }
        return new TrangaTask(TaskAction, reoccurrence);
    }
    
    public static TrangaTask CreateUpdateChaptersTask(ref Dictionary<Publication, Chapter[]> chapterCollection, Connector connector, Publication publication, string language, TimeSpan reoccurrence)
    {
        Dictionary<Publication, Chapter[]> pChapterCollection = chapterCollection;

        void TaskAction()
        {
            Chapter[] chapters = connector.GetChapters(publication, language);
            if(pChapterCollection.TryAdd(publication, chapters))
                pChapterCollection[publication] = chapters;
        }
        return new TrangaTask(TaskAction, reoccurrence);
    }

    public static TrangaTask CreateUpdatePublicationsTask(ref Dictionary<Publication, Chapter[]> chapterCollection, Connector connector, TimeSpan reoccurrence)
    {
        Dictionary<Publication, Chapter[]> pChapterCollection = chapterCollection;

        void TaskAction()
        {
            Publication[] publications = connector.GetPublications();
            foreach (Publication publication in publications)
                pChapterCollection.TryAdd(publication, Array.Empty<Chapter>());
        }
        return new TrangaTask(TaskAction, reoccurrence);
    }
}