using API.Schema.Jobs;
using JobWorker.Jobs;

namespace JobWorker;

internal class Worker
{
    internal readonly Job Job;
    internal readonly Task Task;
    internal Job[] NewJobs;

    public Worker(Job job)
    {
        this.Job = job;
        this.NewJobs = [];
        switch (job.JobType)
        {
            case JobType.CreateArchiveJob:
                CreateArchiveJob caj = job as CreateArchiveJob ?? throw new InvalidOperationException();
                CreateArchive ca = new(caj);
                this.Task = new Task(() => ca.Execute(out NewJobs));
                break;
            case JobType.MoveFileOrFolderJob:
                MoveFileOrFolderJob mfj = job as MoveFileOrFolderJob ?? throw new InvalidOperationException();
                MoveFileOrFolder mf = new(mfj);
                this.Task = new Task(() => mf.Execute(out NewJobs));
                break;
            case JobType.ProcessImagesJob:
                ProcessImagesJob pij = job as ProcessImagesJob ?? throw new InvalidOperationException();
                ProcessImages pi = new(pij);
                this.Task = new Task(() => pi.Execute(out NewJobs));
                break;
            case JobType.DownloadNewChaptersJob:
                DownloadNewChaptersJob dncj = job as DownloadNewChaptersJob ?? throw new InvalidOperationException();
                DownloadNewChapters dnc = new(dncj);
                this.Task = new Task(() => dnc.Execute(out NewJobs));
                break;
            case JobType.DownloadSingleChapterJob:
                DownloadSingleChapterJob dscj = job as DownloadSingleChapterJob ?? throw new InvalidOperationException();
                DownloadSingleChapter dsc = new(dscj);
                this.Task = new Task(() => dsc.Execute(out NewJobs));
                break;
            case JobType.UpdateMetaDataJob:
                UpdateMetadataJob umj = job as UpdateMetadataJob ?? throw new InvalidOperationException();
                UpdateMetadata um = new(umj);
                this.Task = new Task(() => um.Execute(out NewJobs));
                break;
            case JobType.CreateComicInfoXmlJob:
                CreateComicInfoXmlJob ccixj = job as CreateComicInfoXmlJob ?? throw new InvalidOperationException();
                CreateComicInfoXml ccix = new(ccixj);
                this.Task = new Task(() => ccix.Execute(out NewJobs));
                break;
            case JobType.SearchManga:
                SearchMangaJob smj = job as SearchMangaJob ?? throw new InvalidOperationException();
                SearchManga sm = new(smj);
                this.Task = new Task(() => sm.Execute(out NewJobs));
                break;
            default:
                throw new KeyNotFoundException($"Could not create Worker for Job-type {job.JobType}");
        }
        this.Task.Start();
    }
}