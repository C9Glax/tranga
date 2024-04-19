﻿<span id="top"></span>
# Tranga API Calls v2
This document outlines all different HTTP API calls that Tranga accepts.
Tranga expects specific HTTP methods for its calls and therefore careful attention must be paid when making them. 

`apiUri` refers to your `http(s)://TRANGA.FRONTEND.URI/api`.

Parameters are included in the HTTP request URI and/or the request body.
The request Body is in JSON key-value-pair format, with all values as strings.
Tranga responses are always in the JSON format within the Response Body.

Parameters in *italics* are optional

<!-- ### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `` -->
<!-- ### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `` -->
<!-- ### <sub>![DELETE](https://img.shields.io/badge/DELETE-f00)</sub> `` -->

### Quick Entry

* [Connectors](#connectors-top)
* [Manga](#manga-top)
* [Jobs](#jobs-top)
* [Settings](#settings-top)
* [Library Connectors](#library-connectors-top)
* [Notification Connectors](#notification-connectors-top)
* [Miscellaneous](#miscellaneous-top)

## Connectors <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Connector/Types`

Returns available Manga Connectors (Scanlation sites)

<details>
  <summary>Returns</summary>

  List of strings with Names.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Connector/<ConnectorName>/GetManga`

Returns the Manga from the specified Manga Connector.

<details>
  <summary>Request</summary>
  
  `ConnectorName` is returned in the response of [GET /v2/Connector/Types](#-v2connectortypes)
  
  Use either `title` or `url` Parameter.
  
  | Parameter | Value                                           |
  |-----------|-------------------------------------------------|
  | title     | Search Term                                     |
  | url       | Direct link (URL) to the Manga on the used Site |
</details>

<details>
  <summary>Returns</summary>
  
  List of [Manga](Types.md#Manga)
  
  | StatusCode | Meaning                  |
  |------------|--------------------------|
  | 400        | Connector does not exist |
  | 404        | URL/Connector Mismatch   |
</details>

## Manga <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Manga/<internalId>`

Returns the specified Manga.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>
  
  [Manga](Types.md#manga)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
</details>

### <sub>![DELETE](https://img.shields.io/badge/DELETE-f00)</sub> `/v2/Manga/<internalId>`

Deletes all associated Jobs for the specified Manga

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 200        | Jobs were deleted                          |
  | 404        | Manga with `internalId` could not be found |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Manga/<internalId>/Cover`

Returns the URL for the Cover of the specified Manga.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  *  [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>

String with the url.
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Manga/<internalId>/Chapters`

Returns the Chapter-list for the specified Manga.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>
  
  List of [Chapters](Types.md/#chapter)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
</details>

## Jobs <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Jobs`

Returns all configured Jobs.

<details>
  <summary>Returns</summary>
  
  List of JobIds.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Jobs/Running`

Returns all Running Jobs.

<details>
  <summary>Returns</summary>

  List of JobIds.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Jobs/Waiting`

Returns all Waiting Jobs.

<details>
  <summary>Returns</summary>

  List of JobIds.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Jobs/Monitoring`

Returns all Monitoring Jobs.

<details>
  <summary>Returns</summary>

  List of JobIds.
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Jobs/Create/Monitor/<internalId>`

Creates a Monitoring-Job for the specified Manga at the specified Interval.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)

    | Parameter | Value                                                  |
    |-----------|--------------------------------------------------------|
    | interval  | Interval at which the Job is re-run in HH:MM:SS format |
</details>

<details>
  <summary>Returns</summary>
  
  [Job](Types.md#job)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
  | 500        | Error parsing interval                     |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Jobs/Create/DownloadNewChapters/<internalId>`

Creates a Job to check for new Chapters and Download new ones of the specified Manga.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>
  
  [Job](Types.md#job)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Jobs/Create/UpdateMetadata`

Creates a Job to update the Metadata of all Manga.

<details>
  <summary>Returns</summary>
  
  [Job](Types.md#job)
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Jobs/Create/UpdateMetadata/<internalId>`

Updates the Metadata of the specified Manga.

<details>
  <summary>Request</summary>
  
  `internalId` is returned in the response of
  * [GET /v2/Connector/*ConnectorName*/GetManga](#-v2connectorconnectornamegetmanga)
  * [GET /v2/Jobs/*jobId*](#-v2jobs)
</details>

<details>
  <summary>Returns</summary>
  
  [Job](Types.md#job)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Manga with `internalId` could not be found |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Job/<jobId>`

Returns the specified Job.

<details>
  <summary>Request</summary>
  
  `jobId` is returned in the response of
  * [GET /v2/Jobs](#-v2jobs)
  * [GET /v2/Jobs/Running](#-v2jobsrunning)
  * [GET /v2/Jobs/Waiting](#-v2jobswaiting)
  * [GET /v2/Jobs/Monitoring](#-v2jobsmonitoring)
</details>

<details>
  <summary>Returns</summary>
  
  [Job](Types.md#job)
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 404        | Manga with `jobId` could not be found |
</details>

### <sub>![DELETE](https://img.shields.io/badge/DELETE-f00)</sub> `/v2/Job/<jobId>`

Deletes the specified Job and all descendants.

<details>
  <summary>Request</summary>
  
  `jobId` is returned in the response of
  * [GET /v2/Jobs](#-v2jobs)
  * [GET /v2/Jobs/Running](#-v2jobsrunning)
  * [GET /v2/Jobs/Waiting](#-v2jobswaiting)
  * [GET /v2/Jobs/Monitoring](#-v2jobsmonitoring)
</details>

<details>
  <summary>Returns</summary>
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 200        | Job deleted                           |
  | 404        | Manga with `jobId` could not be found |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Job/<jobId>/Progress`

Returns the progress the of the specified Job.

<details>
  <summary>Request</summary>
  
  `jobId` is returned in the response of
  * [GET /v2/Jobs](#-v2jobs)
  * [GET /v2/Jobs/Running](#-v2jobsrunning)
  * [GET /v2/Jobs/Waiting](#-v2jobswaiting)
  * [GET /v2/Jobs/Monitoring](#-v2jobsmonitoring)
</details>

<details>
  <summary>Returns</summary>

  [ProgressToken](Types.md#progresstoken)
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 404        | Manga with `jobId` could not be found |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Job/<jobId>/StartNow`

Starts the specified Job.

<details>
  <summary>Request</summary>
  
  `jobId` is returned in the response of
  * [GET /v2/Jobs](#-v2jobs)
  * [GET /v2/Jobs/Running](#-v2jobsrunning)
  * [GET /v2/Jobs/Waiting](#-v2jobswaiting)
  * [GET /v2/Jobs/Monitoring](#-v2jobsmonitoring)
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 200        | Job started                           |
  | 404        | Manga with `jobId` could not be found |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Job/<jobId>/Cancel`

Cancels the specified Job, or dequeues it.

<details>
  <summary>Request</summary>

  `jobId` is returned in the response of
  * [GET /v2/Jobs](#-v2jobs)
  * [GET /v2/Jobs/Running](#-v2jobsrunning)
  * [GET /v2/Jobs/Waiting](#-v2jobswaiting)
  * [GET /v2/Jobs/Monitoring](#-v2jobsmonitoring)
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 200        | Job cancelled                         |
  | 404        | Manga with `jobId` could not be found |
</details>

## Settings <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings`

Returns the `settings.json` file.

<details>
  <summary>Returns</summary>

  [Settings](Types.md#settings)
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings/UserAgent`

Returns the current User Agent used for Requests.

<details>
  <summary>Returns</summary>

  [UserAgent](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent)
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Settings/UserAgent`

Sets the User Agent. If left empty, User Agent is reset to default.

<details>
  <summary>Request</summary>

| Parameter | Value                                                                                  |
|-----------|----------------------------------------------------------------------------------------|
| value     | New [UserAgent](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent)  |
</details>

<details>
  <summary>Returns</summary>
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings/RateLimit/Types`

Returns the configurable Rate-Limits.

<details>
  <summary>Returns</summary>

  List of Rate-Limit-Names.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings/RateLimit`

Returns the current configuration of Rate-Limits for Requests.

<details>
  <summary>Returns</summary>

  Dictionary of `Rate-Limits` and `Requests per Minute`
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Settings/RateLimit`

Sets the Rate-Limits for all Requests. If left empty, resets to default Rate-Limits.

<details>
  <summary>Request</summary>

  For each Rate-Limit set as follows:
  
  | Parameter                          | Value               |
  |------------------------------------|---------------------|
  | [Type](#-v2settingsratelimittypes) | Requests per Minute |
  
  `Type` is returned by [GET /v2/Settings/RateLimit/Types](#-v2settingsratelimittypes)
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                        |
  |------------|--------------------------------|
  | 404        | Rate-Limit-Name does not exist |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings/RateLimit/<Type>`

Returns the current Rate-Limit for the Request-Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/Settings/RateLimit/Types](#-v2settingsratelimittypes)
</details>

<details>
  <summary>Returns</summary>

  Integer with Requests per Minute.
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Settings/RateLimit/<Type>`

Sets the Rate-Limit for the Request-Type in Requests per Minute.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/Settings/RateLimit/Types](#-v2settingsratelimittypes)
  
  | Parameter | Value               |
  |-----------|---------------------|
  | value     | Requests per Minute |
</details>
<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                        |
  |------------|--------------------------------|
  | 404        | Rate-Limit-Name does not exist |
  | 500        | Parsing Error                  |
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Settings/AprilFoolsMode`

Returns the current state of the April-Fools-Mode setting.

<details>
  <summary>Returns</summary>

  Boolean
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Settings/ApriFoolsMode`

Enables/Disables April-Fools-Mode.

<details>
  <summary>Request</summary>

  | Parameter | Value      |
  |-----------|------------|
  | value     | true/false |
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                        |
  |------------|--------------------------------|
  | 404        | Rate-Limit-Name does not exist |
  | 500        | Parsing Error                  |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Settings/DownloadLocation`

Updates the default Download-Location.

<details>
  <summary>Request</summary>

  | Parameter   | Value            |
  |-------------|------------------|
  | location    | New Folder-Path  |
  | *moveFiles* | __*true*__/false |
</details>

<details>
  <summary>Returns</summary>


  | StatusCode | Meaning                  |
  |------------|--------------------------|
  | 200        | Successfully changed     |
  | 500        | Files could not be moved |
</details>

## Library Connectors <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/LibraryConnector`

Returns the configured Library-Connectors.

<details>
  <summary>Returns</summary>

  List of [LibraryConnectors](Types.md#libraryconnector)
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/LibraryConnector/Types`

Returns the available Library-Connector types.

<details>
  <summary>Returns</summary>

  List of String of Names.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/LibraryConnector/<Type>`

Returns the Library-Connector for the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/LibraryConnector/Types](#-v2libraryconnectortypes)
</details>

<details>
  <summary>Returns</summary>

  [LibraryConnector](Types.md#libraryconnector)
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 404        | Library Connector Type does not exist |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/LibraryConnector/<Type>`

Creates a Library-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/LibraryConnector/Types](#-v2libraryconnectortypes)
  
  | Parameter   | Value              |
  |-------------|--------------------|
  | URL         | URL of the Library |
  
  #### Type specific Parameters (must be included for each)
  * Komga
  
  | Parameter | Value                                                                                                             |
  |-----------|-------------------------------------------------------------------------------------------------------------------|
  | auth      | [Base64 encoded Basic-Authentication-String](https://datatracker.ietf.org/doc/html/rfc7617) (`username:password`) |
  
  * Kavita
  
  | Parameter | Value           |
  |-----------|-----------------|
  | username  | Kavita Username |
  | password  | Kavita Password |
</details>

<details>
  <summary>Returns</summary>

  [LibraryConnector](Types.md#libraryconnector)
  
  | StatusCode | Meaning                          |
  |------------|----------------------------------|
  | 404        | Library Connector does not exist |
  | 500        | Parsing Error                    |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/LibraryConnector/<Type>/Test`

Tests a Library-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/LibraryConnector/Types](#-v2libraryconnectortypes)
  
  | Parameter   | Value              |
  |-------------|--------------------|
  | URL         | URL of the Library |

  #### Type specific Parameters (must be included for each)
  * Komga
  
  | Parameter | Value                                                                                                             |
  |-----------|-------------------------------------------------------------------------------------------------------------------|
  | auth      | [Base64 encoded Basic-Authentication-String](https://datatracker.ietf.org/doc/html/rfc7617) (`username:password`) |
  
  * Kavita
  
  | Parameter | Value           |
  |-----------|-----------------|
  | username  | Kavita Username |
  | password  | Kavita Password |
  </details>
  
  <details>
    <summary>Returns</summary>
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 200        | Test successful                       |
  | 404        | Library Connector Type does not exist |
  | 408        | Test failed                           |
  | 500        | Parsing Error                         |
</details>

### <sub>![DELETE](https://img.shields.io/badge/DELETE-f00)</sub> `/v2/LibraryConnector/<Type>`

Deletes the Library-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/LibraryConnector/Types](#-v2libraryconnectortypes)
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 200        | Deleted                               |
  | 404        | Library Connector Type does not exist |
</details>

## Notification Connectors <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/NotificationConnector`

Returns the configured Notification-Connectors.

<details>
  <summary>Returns</summary>

  List of [NotificationConnectors](Types.md#notificationconnector)
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/NotificationConnector/Types`

Returns the available Notification-Connectors.

<details>
  <summary>Returns</summary>
  
  List of String of Names.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/NotificationConnector/<Type>`

Returns the configured Notification-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/NotificationConnector/Types](#-v2notificationconnectortypes)
</details>

<details>
  <summary>Returns</summary>

  [Notification Connector](Types.md#notificationconnector)
  
  | StatusCode | Meaning                               |
  |------------|---------------------------------------|
  | 404        | Library Connector Type does not exist |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/NotificationConnector/<Type>`

Creates a Notification-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/NotificationConnector/Types](-v2notificationconnectortypes)
  
  #### Type specific Parameters (must be included for each)
  * Gotify
  
  | Parameter | Value                                 |
  |-----------|---------------------------------------|
  | url       | URL of the Gotify Instance            |
  | appToken  | AppToken of the configured Gotify App |
  
  * LunaSea
  
  | Parameter | Value           |
  |-----------|-----------------|
  | webhook   | LunaSea Webhook |
  
  * Nty
  
  | Parameter | Value                    |
  |-----------|--------------------------|
  | url       | URL of the Ntfy Instance |
  | auth      | Auth-String              |
</details>

<details>
  <summary>Returns</summary>

  [NotificationConnector](Types.md#notificationconnector)
  
  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 404        | Notification Connector Type does not exist |
  | 500        | Parsing Error                              |
</details>

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/NotificationConnector/<Type>/Test`

Tests a Notification-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/NotificationConnector/Types](#-v2notificationconnectortypes)
  
  #### Type specific Parameters (must be included for each)
  * Gotify
  
  | Parameter | Value                                 |
  |-----------|---------------------------------------|
  | url       | URL of the Gotify Instance            |
  | appToken  | AppToken of the configured Gotify App |
  
  * LunaSea
  
  | Parameter | Value           |
  |-----------|-----------------|
  | webhook   | LunaSea Webhook |
  
  * Ntfy
  
  | Parameter | Value                    |
  |-----------|--------------------------|
  | url       | URL of the Ntfy Instance |
  | auth      | Auth-String              |
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 200        | Test successful                            |
  | 404        | Notification Connector Type does not exist |
  | 408        | Test failed                                |
  | 500        | Parsing Error                              |
</details>

### <sub>![DELETE](https://img.shields.io/badge/DELETE-f00)</sub> `/v2/NotificationConnector/<Type>`

Deletes the Notification-Connector of the specified Type.

<details>
  <summary>Request</summary>

  `Type` is returned by [GET /v2/NotificationConnector/Types](#-v2notificationconnectortypes)
</details>

<details>
  <summary>Returns</summary>

  | StatusCode | Meaning                                    |
  |------------|--------------------------------------------|
  | 200        | Deleted                                    |
  | 404        | Notification Connector Type does not exist |
</details>

## Miscellaneous <sup>[^top](#top)</sup>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/LogFile`

Returns the current log-file.

<details>
  <summary>Returns</summary>

  The Logfile as Stream.
</details>

### <sub>![GET](https://img.shields.io/badge/GET-0f0)</sub> `/v2/Ping`

Pong!

### <sub>![POST](https://img.shields.io/badge/POST-00f)</sub> `/v2/Ping`

Pong!