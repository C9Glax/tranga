﻿namespace API.Schema.Jobs;


public enum JobType : byte
{
    DownloadSingleChapterJob = 0, 
    DownloadNewChaptersJob = 1, 
    UpdateMetaDataJob = 2,
    MoveFileOrFolderJob = 3,
    DownloadMangaCoverJob = 4
}