using API.Schema.Jobs;
using JobWorker.Jobs;

namespace JobWorker;

internal class Worker
{
    internal readonly Job Job;
    internal readonly Task Task;

    public Worker(Job job)
    {
        this.Job = job;
        switch (job.JobType)
        {
            case JobType.CreateArchiveJob:
                CreateArchiveJob caj = job as CreateArchiveJob ?? throw new InvalidOperationException();
                CreateArchive ca = new();
                this.Task = new Task(() => ca.Execute(caj.Chapter, caj.DependsOnJobs));
                break;
            case JobType.MoveFileOrFolderJob:
                MoveFileOrFolderJob mfj = job as MoveFileOrFolderJob ?? throw new InvalidOperationException();
                MoveFileOrFolder mf = new();
                this.Task = new Task(() => mf.Execute((mfj.FromLocation, mfj.ToLocation), mfj.DependsOnJobs));
                break;
            case JobType.ProcessImagesJob:
                ProcessImagesJob pij = job as ProcessImagesJob ?? throw new InvalidOperationException();
                ProcessImages pi = new();
                this.Task = new Task(() => pi.Execute((pij.Path, pij.Bw, pij.Compression), pij.DependsOnJobs));
                break;
            case JobType.DownloadNewChaptersJob:
                DownloadNewChaptersJob dncj = job as DownloadNewChaptersJob ?? throw new InvalidOperationException();
                DownloadNewChapters dnc = new();
                this.Task = new Task(() => dnc.Execute(dncj.Manga, dncj.DependsOnJobs));
                break;
            case JobType.DownloadSingleChapterJob:
                DownloadSingleChapterJob dscj = job as DownloadSingleChapterJob ?? throw new InvalidOperationException();
                DownloadSingleChapter dsc = new();
                this.Task = new Task(() => dsc.Execute(dscj.Chapter, dscj.DependsOnJobs));
                break;
            case JobType.UpdateMetaDataJob:
                UpdateMetadataJob umj = job as UpdateMetadataJob ?? throw new InvalidOperationException();
                UpdateMetadata um = new();
                this.Task = new Task(() => um.Execute(umj.Manga, umj.DependsOnJobs));
                break;
            case JobType.CreateComicInfoXmlJob:
                CreateComicInfoXmlJob cixj = job as CreateComicInfoXmlJob ?? throw new InvalidOperationException();
                CreateComicInfoXml cix = new();
                this.Task = new Task(() => cix.Execute(cixj.Chapter, cixj.DependsOnJobs));
                break;
            default:
                throw new KeyNotFoundException($"Could not create Worker for Job-type {job.JobType}");
        }
        this.Task.Start();
    }
}