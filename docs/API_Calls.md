## Tranga API Calls
This document serves to outline all of the different HTTP API calls that Tranga accepts. Tranga expects specific HTTP methods for its calls and therefore careful attention must be paid when making them. 
In the examples below, `{apiUri}` refers to your `http(s)://TRANGA.FRONTEND.URI/api`. Parameters are included in the HTTP request URI and the request body is in JSON format. Tranga responses are always
in the JSON format within the Response Body.

#### [GET] /Connectors
Retrieves the available manga sites (connectors) that Tranga is currently able to download manga from.

- Parameters: 
	None

- Request Body: 
	None
	
#### [GET] /Jobs
Retrieves all jobs that Tranga is keeping track of, includes Running Jobs, Waiting Jobs, Manga Tracking (Monitoring) Jobs.

- Parameters:
	None
	
- Request Body: 
	None
	
#### [DELETE] /Jobs
Removes the specified job given by the job ID

- Request Variables: 
	- None
	
- Request Body:
	```
	{
		jobId: ${Tranga Job ID}
	}
	```

#### [POST] /Jobs/Cancel
Cancels a running job or prevents a queued job from running.

- Parameters: 
	None
	
- Request Body:
	```
	{
		jobId: ${Tranga Job ID}
	}
	```
	
#### [POST] /Jobs/DownloadNewChapters
Manually adds a Job to Tranga's queue to check for and download new chapters for a specified manga

- Parameters:
	None

- Request Body: 
	```
	{
		connector: ${Manga Connector to Download From}
		internalId: ${Tranga Manga ID}
		translatedLanguage: ${Manga Language}
	}
	```

#### [GET] /Jobs/Running
Retrieves all currently running jobs.

- Parameters:
	None
	
- Request Body: 
	None
	
#### [POST] /Jobs/StartNow
Manually starts a configured job 
- Parameters:
	None
	
- Request Body:
	```
	{
		jobId: ${Tranga Job ID}
	}
	```
	
#### [GET]/Jobs/Waiting
Retrieves all currently queued jobs.

- Parameters:
	None
	
- Request Body:
	None
	
#### [GET] /Jobs/MonitorJobs
Retrieves all jobs for Mangas that Tranga is currently tracking.

- Parameters:
	None
	
- Request Body:
	None
	
#### [POST] /Jobs/MonitorManga
Adds a new manga for Tranga to monitor

- Parameters: 
	None
	
- Request Body: 
	```
	{
		connector: ${Manga Connector to download from}
		internalId: ${Tranga Manga ID}
		interval: ${Interval at which to run job, in the HH:MM:SS format}
		translatedLanguage: ${Supported language code}
		ignoreBelowChapterNum: ${Chapter number to start downloading from}
		customFolderName: ${Folder Name to save Manga to}
	}
	```
	
#### [GET] /Jobs/Progress
Retrieves the current completion progress of a running or waiting job. Tranga's ID for the Job is returned with each of the `GET /Job/` API calls.

- Parameters: 
	- `{jobId}`: Tranga Job ID 
	
- Request Body:
	None
	
#### [POST] /Jobs/UpdateMetadata
Updates the metadata for all monitored mangas

- Parameters:
	None
	
- Request Body:
	None
	
#### [GET] /LibraryConnectors
Retrieves the currently configured library servers

- Parameters:
	None
	
- Request Body:
	None

#### [DELETE] /LibraryConnectors/Reset
Resets or clears a configured library connector

- Parameters:
	None

- Request Body: 
	```
	{
		libraryConnector: Komga/Kavita
	}
	```
	
#### [POST] /LibraryConnectors/Test
Verifies the behavior of a library connector before saving it. The connector must be checked to verify that the connection is active.


- Parameters:
	None
	
- Request Body: 
	```
	{
		libraryConnector: Komga/Kavita
		libraryURL: ${Library URL}
		komgaAuth: Only for when libraryConnector = Komga
		kavitaUsername: Only for when libraryConnector = Kavita
		kavitaPassword: Only for when libraryConnector = Kavita
	}
	```

#### [GET] /LibraryConnectors/Types
Retrives Key-Value pairs for all of Tranga's currently supported library servers.

- Parameters:
	None
	
- Request Body:
	None
	
#### [POST] /LibraryConnectors/Update
Updates or Adds a Library Connector to Tranga

- Parameters: None

- Request Body: 
	```
	{
		libraryConnector: Komga/Kavita
		libraryURL: ${Library URL}
		komgaAuth: Only for when libraryConnector = Komga
		kavitaUsername: Only for when libraryConnector = Kavita
		kavitaPassword: Only for when libraryConnector = Kavita
	}
	```
	
#### [GET] /LogFile
Retrieves the log file from the running Tranga instance

- Parameters:
	None
	
- Request Body:
	None
	
#### [GET] /Manga/FromConnector
Retrieves the details about a specified manga from a specific connector. If the manga title returned by Tranga is a URL (determined by the presence of `http` in the title, the API call should use the second 
call with the `url` rather than the `title`.

- Parameters:
	- `{connector}`: Manga Connector 
	- `{url/title}`: Manga URL/Title

- Request Body:
	None

#### [GET] /Manga/Chapters
Retrieves the currently available chapters for a specified manga from a connector. The `{internalId}` is how Tranga uniquely recognizes and distinguishes different Manga. 

- Parameters: 
	- `{connector}`: Manga Connector 
	- `{internalId}`: Tranga Manga ID 
	- `{translatedLanguage}`: Translated Language 
 
- Request Body:
	None
	
#### [GET] /Manga/Cover
Retrives the URL of the cover image for a specific manga that Tranga is tracking.

- Parameters: 
	- `{internalId}`: Tranga Manga ID 
	
- Request Body:
	None
	
#### [GET] /NotificationConnectors
Retrieves the currently configured notification providers

- Parameters:
	None
	
- Request Body:
	None
	
#### [DELETE] /NotificationConnectors/Reset
Resets or clears a configured notification connector

- Parameters:
	None
	
- Request Body: 
	```
	{
		notificationConnector: Gotify/Ntfy/LunaSea
	}
	```
	
#### [POST] /NotificationConnectors/Test
Tests a notification connector with the currently input settings. The connector behavior must be checked to verify that the input settings are correct.

- Parameters:
	None
	
- Request Body:
	```
	{
		notificationConnector: Gotify/Ntfy/LunaSea
		
		gotifyUrl: 
		gotifyAppToken: 
		
		lunaseaWebhook: 
		
		ntfyUrl:
		ntfyAuth:
	}
	```
	
#### [POST] /NotificationConnectors/Update
Updates or Adds a notification connector to Tranga

- Parameters:
	None
	
- Request Body:
	```
	{
		notificationConnector: Gotify/Ntfy/LunaSea
		
		gotifyUrl: 
		gotifyAppToken: 
		
		lunaseaWebhook: 
		
		ntfyUrl:
		ntfyAuth:
	}
	```

#### [GET] /NotificationConnectors/Types
Retrives Key-Value pairs for all of Tranga's currently supported notification providers.

- Parameters: 
	None
	
- Request Body: 
	None
	
#### [GET] /Ping
This call is used periodically by the web frontend to establish that connection to the server is active.

- Parameters: 
	None
	
- Request Body: 
	None

#### [GET] /Settings
Retrieves the content of Tranga's `settings.json`

- Parameters: 
	None
	
- Request Body: 
	None
	
#### [GET] /Settings/customRequestLimit
Retrieves the configured rate limits for different types of manga connector requests.

- Parameters: 
	None
	
- Request Body: 
	None
	
#### [POST] /Settings/customRequestLimit
Sets the rate limits for different types of manga connector requests.

- Parameters:
	None

- Request Body:
	```
	{
		requestType: {Request Byte}
		requestsPerMinute: {Rate Limit in Requests Per Minute}
	}
	```
	
#### [POST] /Settings/UpdateDownloadLocation
Updates the root directory of where Tranga downloads manga
 
- Parameters:
	None
	
- Request Body:
	```
	{
		downloadLocation: {New Root Directory}
		moveFiles: "true"/"false"
	}
	```
#### [POST] /Settings/userAgent
Updates the user agent that Tranga uses when scraping the web

- Parameters
	
- Request Body:
	```
	{
		userAgent: {User Agent String}
	}
	```
	
