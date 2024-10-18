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
    "releaseStatus": ReleaseStatus,
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
    jobType: number,
    mangaInternalId: string,
    translatedLanguage: string,
    progressToken: ProgressToken,
    recurring: boolean,
    recurrenceTime: string,
    lastExecution: Date,
    nextExecution: Date,
    id: string,
    parentJobId: string | null,
    mangaConnector: Connector
}
```

## ProgressToken
```
{
    cancellationRequested: boolean,
    increments: number,
    incrementsCompleted: number,
    progress: number,
    lastUpdate: Date,
    executionStarted: Date,
    timeRemaining: Date,
    state: number
}
```

## Settings
```
{
}
```

## LibraryConnector
```
{
}
```

## NotificationConnector
```
{
}
```