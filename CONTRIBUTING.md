# Contributing
First of, **thank you for considering enhancing the experience for all users** (including me)!

To contribute, create a branch based on `cuttingedge` and make your changes in there.
If you can, create a **Draft**-PR to merge into `cuttingedge`, to track progress, and when you think everything works, mark it as ready.
Since `cuttingedge` frequently changes, pulling frequently is suggested, to avoid conflicts with changes in other classes (For example `Manga`, `Chapter`, `MangaConnector`).

## Contributing a Connector
To contribute a new `MangaConnector`, inherit from this class and implement the abstract methods.
Orient yourself on any of the existing connectors. If there is a REST-Api, use it and observe Rate-Limits for Requests.
The `downloadClient` should be `HTMLDownloadClient` unless the pages served by the Website are dynamically generated. This helps with memory-usage and loadtimes.

**Overarching-Classes:**

Nullable fields should be filled as long as possible.
- `Manga`
  - `publicationId` is a unique identifier of a Manga. It sould be part of the URL, used for future requests
  - `posterUrl` is a URL from the website. `coverFileNameInCache` should be requested by calling `SaveCoverImageToCache()`
- `Chapter`s shall ALWAYS have a unique ChapterNumber.
  - If there is a unique identifier (`id`) for a Chapter, other than the ChapterNumber, please include it, to avoid duplication
