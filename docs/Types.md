## Connector

```
{
  "name": string,
  "SupportedLanguages": string[],
  "BaseUris": string[]
}
```

## Manga
```
{
    "sortName": string,
    "authors": string[],
    "altTitles": string[][],
    "description": string,
    "tags": string[],
    "coverUrl": string,
    "coverFileNameInCache": string,
    "links": string[][],
    "year": int,
    "originalLanguage": string,
    "releaseStatus": ReleaseStatus, see ReleaseStatus
    "folderName": string,
    "publicationId": string,
    "internalId": string,
    "ignoreChaptersBelow": number,
    "latestChapterDownloaded": number,
    "latestChapterAvailable": number,
    "websiteUrl": string,
    "mangaConnector": Connector
}
```

## Chapter
```
{
    "parentManga": IManga,
    "name": string | undefined,
    "volumeNumber": string,
    "chapterNumber": string,
    "url": string,
    "fileName": string
}
```

## ReleaseStatus
```
{
    Continuing = 0,
    Completed = 1,
    OnHiatus = 2,
    Cancelled = 3,
    Unreleased = 4
}
```

## Job
```
{
    "progressToken": IProgressToken,
    "recurring": boolean,
    "recurrenceTime": string,
    "lastExecution": Date,
    "nextExecution": Date,
    "id": string,
    "jobType": number, //see JobType
    "parentJobId": string | null,
    "mangaConnector": IMangaConnector,
    "mangaInternalId": string | undefined, //only on DownloadNewChapters
    "translatedLanguage": string | undefined, //only on DownloadNewChapters
    "chapter": IChapter | undefined, //only on DownloadChapter
}
```

## JobType
```
{
    DownloadChapterJob = 0,
    DownloadNewChaptersJob = 1,
    UpdateMetaDataJob = 2,
    MonitorManga = 3
}
```

## ProgressToken
```
{
    "cancellationRequested": boolean,
    "increments": number,
    "incrementsCompleted": number,
    "progress": number,
    "lastUpdate": Date,
    "executionStarted": Date,
    "timeRemaining": Date,
    "state": number
}
```

## Settings
```
{
    "downloadLocation": string,
    "workingDirectory": string,
    "apiPortNumber": number,
    "userAgent": string,
    "bufferLibraryUpdates": boolean,
    "bufferNotifications": boolean,
    "version": number,
    "aprilFoolsMode": boolean,
    "requestLimits": {
        "MangaInfo": number,
        "MangaDexFeed": number,
        "MangaDexImage": number,
        "MangaImage": number,
        "MangaCover": number,
        "Default": number
    }
}
```

## LibraryConnector
```
{
    "libraryType": number, //see LibraryType
    "baseUrl": string,
    "auth": string
}
```

## LibraryType
```
{
    Komga = 0,
    Kavita = 1
}
```

## NotificationConnector
```
{
}
```