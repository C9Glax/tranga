namespace API.Schema.Jobs;


public enum JobType : byte
{
    DownloadSingleChapterJob = 0, 
    DownloadNewChaptersJob = 1, 
    UpdateMetaDataJob = 2,
    MoveFileJob = 3,
    CreateArchiveJob = 4,
    ProcessImagesJob = 5,
    CreateComicInfoXmlJob = 6
}