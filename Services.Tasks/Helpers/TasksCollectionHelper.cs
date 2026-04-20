using Services.Tasks.TaskTypes;

namespace Services.Tasks.Helpers;

internal static class TasksCollectionHelper
{
    internal static IEnumerable<IMangaTask> RelatedToManga(this IEnumerable<TaskBase> collection, Guid mangaId) =>
        collection.OfType<IMangaTask>().Where(t => t.MangaId == mangaId);
}