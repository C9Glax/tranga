using API.Schema.Jobs;

namespace API;

internal static class JobQueueSorter
{
    public static readonly Dictionary<JobType, byte> JobTypePriority = new()
    {

        { JobType.DownloadSingleChapterJob, 50 },
        { JobType.DownloadAvailableChaptersJob, 51 },
        { JobType.MoveFileOrFolderJob, 102 },
        { JobType.DownloadMangaCoverJob, 10 },
        { JobType.RetrieveChaptersJob, 52 },
        { JobType.UpdateChaptersDownloadedJob, 90 },
        { JobType.MoveMangaLibraryJob, 101 },
        { JobType.UpdateCoverJob, 11 },
    };

    public static byte GetPriority(Job job)
    {
        return JobTypePriority[job.JobType];
    }

    public static byte GetPriority(JobType jobType)
    {
        return JobTypePriority[jobType];
    }
    
    public static IEnumerable<Job> Sort(this IEnumerable<Job> jobQueueSortables)
    {
        return jobQueueSortables.Order();
    }

    public static IEnumerable<Job> GetStartableJobs(this IEnumerable<Job> jobQueueSortables)
    {
        Job[] sorted = jobQueueSortables.Order().ToArray();
        // Job has to be due, no missing dependenices
        // Index - 1, Index is first job that does not match requirements
        IEnumerable<(int Index, Job Item)> index = sorted.Index();
        (int i, Job? item) = index.FirstOrDefault(job =>
            job.Item.NextExecution > DateTime.UtcNow || job.Item.GetDependencies().Any(j => !j.IsCompleted));
        if (item is null)
            return sorted;
        index.
    }
}