﻿namespace API.Schema.Jobs;


public enum JobType : byte
{
    DownloadSingleChapterJob = 0, 
    DownloadAvailableChaptersJob = 1, 
    UpdateMetaDataJob = 2,
    MoveFileOrFolderJob = 3,
    DownloadMangaCoverJob = 4,
    RetrieveChaptersJob = 5,
    UpdateFilesDownloadedJob = 6
}