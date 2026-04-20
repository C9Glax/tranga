namespace Services.Tasks.Entities;

public interface IRunOnceTask;

public record RunOnceTask : Task, IRunOnceTask;

public record MangaRunOnceTask : MangaTask, IRunOnceTask;

public record ChapterRunOnceTask : ChapterTask, IRunOnceTask;