<span id="readme-top"></span>
<div align="center">

  <h1 align="center">Tranga v2</h1>
  <p align="center">
    Automatic Manga and Metadata downloader 
  </p>
  
  ![GitHub License](https://img.shields.io/github/license/C9glax/tranga)
  
  <table>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/main?label=main"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-main.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/cuttingedge?label=cuttingedge"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-cuttingedge.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/testing?label=testing"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-testing.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/oldstable?label=oldstable"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-oldstable.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
  </table>

</div>

<!-- ABOUT THE PROJECT -->
## About The Project

Tranga can download Chapters and Metadata from "Scanlation" sites such as 

- [MangaDex.org](https://mangadex.org/) (Multilingual)
- [MangaWorld](https://www.mangaworld.cx) (it)
- [AsuraComic](https://asurascanz.com) (en) thanks @yacob841
- ❓ Open an [issue](https://github.com/C9Glax/tranga/issues/new?assignees=&labels=New+Connector&projects=&template=new_connector.yml&title=%5BNew+Connector%5D%3A+)

and trigger a library-scan with [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/).  
Notifications can be sent to your devices using [Gotify](https://gotify.net/), [LunaSea](https://www.lunasea.app/) or [Ntfy](https://ntfy.sh/
), or any other service that can use REST Webhooks.

## What this program does and does *not* do

*DOES*: Download Images from a Website.<br />
*DOES*: Create Archives with those images.<br />

_**how**?_

Tranga (this repository) is a REST-API and worker in one. Tranga provides REST-Endpoints to configure workers (Jobs).
Requests include searches for Manga, creating and starting Jobs such as downloading available chapters.
For available endpoints check `http(s)://<hostedInstance>/swagger` or `API/openapi/API_v2.json`

**This repository** _**does not**_ include a frontend. A frontend can take many forms, such as a website:

[tranga-website](https://github.com/C9Glax/tranga-website) (Original)

[tranga-yacobGUI](https://github.com/yacob841/tranga-yacobGUI) (Third Party)

When downloading a chapter (meaning the images that make-up the manga) from a Website, Tranga will
additionally try and fetch Metadata from the same website or enhance it from third-party sources.
Downloaded images can be jpeg-compressed and/or made black and white to save on diskspace
(measured at least a 50% reduction in size, without a significant loss of quality).

Tranga will then package the contents of each chapter in a `.cbz`-archive and place it in a common folder per Manga.
If specified, Tranga will then notify library-Managers such as [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/) to trigger a scan for new
chapters. Tranga can also send notifications to your devices via third-party services such as [Gotify](https://gotify.net/), [Ntfy](https://ntfy.sh/),
or any other REST Webhook.

## Screenshots

This repository has no frontend, however checkout [tranga-website](https://github.com/C9Glax/tranga-website) for a default!

## Inspiration:

Because [Kaizoku](https://github.com/oae/kaizoku) was relying on [mangal](https://github.com/metafates/mangal) and mangal
hasn't received bugfixes for its issues with Titles not showing up, or throwing errors because of illegal characters,
there were no alternatives for automatic downloads. However, [Kaizoku](https://github.com/oae/kaizoku) certainly had a great Web-UI.

That is why I wanted to create my own project, in a language I understand, and that I am able to maintain myself.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Endpoint Documentation

Checkout `openapi/API_v2.json`

The container also spins up a Swagger site at `http://<url>/swagger`.

## Built With

- ASP.NET
  - EF Core
- [PostgreSQL](https://www.postgresql.org/about/licence/)
- [Ngpsql](https://github.com/npgsql/npgsql/blob/main/LICENSE)
- [Swagger](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
- [Sixlabors.ImageSharp](https://docs-v2.sixlabors.com/articles/imagesharp/index.html#license)
- [Html Agility Pack (HAP)](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [Soenneker.Utils.String.NeedlemanWunsch](https://github.com/soenneker/soenneker.utils.string.needlemanwunsch/blob/main/LICENSE)
- [Jikan](https://jikan.moe/)
  - [Jikan.Net](https://github.com/Ervie/jikan.net)
- [BuildInformation](https://github.com/linkdotnet/BuildInformation)
- 💙 Blåhaj 🦈

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Star History

<a href="https://star-history.com/#c9glax/tranga&Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date" />
 </picture>
</a>

<!-- GETTING STARTED -->
## Getting Started

### Docker

Built for AMD64 (and ARM64, maybe, if it feels like it).

An example `docker-compose.yaml` is provided. Mount `/Manga` to wherever you want your chapters (`.cbz`-Archives)
downloaded (where Komga/Kavita can access them for example).  
The file also includes [tranga-website](https://github.com/C9Glax/tranga-website) as frontend. For its configuration refer to the
[Tranga-Website Repository](https://github.com/C9Glax/tranga-website) README.

| Environment Variable              | default          | Description                                                                                                      |
|-----------------------------------|------------------|------------------------------------------------------------------------------------------------------------------|
| POSTGRES_HOST                     | `tranga-pg:5432` | host-address of postgres database                                                                                |
| POSTGRES_DB                       | `postgres`       | name of database                                                                                                 |
| POSTGRES_USER                     | `postgres`       | username used for database authentication                                                                        |
| POSTGRES_PASSWORD                 | `postgres`       | password used for database authentication                                                                        |
| DOWNLOAD_LOCATION                 | `/Manga`         | Target-Directory for Downloads (Container path!)                                                                 |
| FLARESOLVERR_URL                  | <empty>          | URL of Flaresolverr-Instance                                                                                     |
| POSTGRES_COMMAND_TIMEOUT          | `60`             | [Timeout of Postgres-commands](https://www.npgsql.org/doc/connection-string-parameters.html?q=Command%20Timeout) |
| POSTGRES_CONNECTION_TIMEOUT       | `30`             | Timeout for postgres-databaes connection                                                                         |
| CHECK_CHAPTERS_BEFORE_START       | `true`           | Whether to update database downloaded chapters column                                                            |
| MATCH_EXACT_CHAPTER_NAME          | `true`           | Match the stored filename exactly with the filename on disk when checking if a chapter is downloaded             |
| CREATE_COMICINFO_XML              | `true`           | Whether to include ComicInfo.xml in .cbz-Archives                                                                |
| ALWAYS_INCLUDE_VOLUME_IN_FILENAME | `false`          | Override to always include a volume in filenames (default as `Vol. 0`)                                           |
| HTTP_REQUEST_TIMEOUT              | `10`             | Request timeout for Mangaconnectors                                                                              |
| REQUESTS_PER_MINUTE               | `90`             | Maximum requests per minute for Mangaconnectors                                                                  |
| MINUTES_BETWEEN_NOTIFICATIONS     | `1`              | Interval at which Tranga checks if notifications need to be sent.                                                |
| HOURS_BETWEEN_NEW_CHAPTERS_CHECK  | `3`              | Interval at which Tranga checks if there are new chapters for a manga                                            |
| WORKER_TIMEOUT                    | `600`            | Seconds a worker can take before being forcefully cancelled                                                      |

### Bare-Metal

While not supported/currently built, Tranga should also run Bare-Metal without issue.

Configuration-Files will be stored per OS:
- Linux `/usr/share/tranga-api`
- Windows `%appdata%/tranga-api`

Downloads (default) are stored in - but this can be configured in `settings.json` (which will be generated on first after first launch):
- Linux `/Manga`
- Windows `%currentDirectory%/Downloads`

### Prerequisits

[.NET-Core 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

<!-- CONTRIBUTING -->
## Contributing

If you want to contribute, please feel free to fork and create a Pull-Request!

### General rules

- Strong-type your variables. This improves readability.
    - **DO**
      ```csharp
      Manga[] zyx = Object.GetAnotherThing(); //I can see that zyx is an Array, without digging through more code
      ```
    - **DO _NOT_**
      ```csharp
      var xyz = Object.GetSomething(); //What is xyz? An Array? A string? An object?
      ```

- Indent your `if` and `for` blocks
    - **DO**
      ```csharp
      if(true)
        return false;
      ```
    - **DO _NOT_**
      ```csharp
      if(true) return false;
      ```
      <details>
        <summary>Because try reading this</summary>
      
        ```csharp
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
        ```

      </details>

- When using shorthand, _this_ improves readability for longer lines (at some point just use if-else...):
```csharp
bool retVal = xyz is true
    ? false
    : true;
```
```csharp
bool retVal = xyz?
    ?? abc?
    ?? true;
```

### Database and EF Core

Tranga is using a **code-first** EF-Core approach. If you modify the database(context) structure you need to create a migration.

###### Configuration Environment-Variables:

| variable          | default-value    |
|-------------------|------------------|
| POSTGRES_HOST     | `tranga-pg:5432` |
| POSTGRES_DB       | `postgres`       |
| POSTGRES_USER     | `postgres`       |
| POSTGRES_PASSWORD | `postgres`       |

### A broad overview of where is what:

![Image](DB-Layout.png)

- `Program.cs` Configuration for ASP.NET, Swagger (also in `NamedSwaggerGenOptions.cs`)
- `Tranga.cs` Worker-Logic
- `Schema/**` Entity-Framework Schema Definitions
- `MangaDownloadClients/**` Networking-Clients for Scraping
- `Controllers/**` ASP.NET Controllers (Endpoints)

##### If you want to add a new Website-Connector:

1. Copy one of the existing connectors, or start from scratch and inherit from `API.Schema.MangaConnectors.MangaConnector`.
2. Add the new Connector as Object-Instance in `Tranga.cs` to the MangaConnector-Array `connectors`.

### How to test locally

In the Project root a `docker-compose.local.yaml` file will compile the code and create the container(s).

<!-- LICENSE -->
## License

Distributed under the GNU GPLv3  License. See [LICENSE.txt](https://github.com/C9Glax/tranga/blob/master/LICENSE.txt) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Choose an Open Source License](https://choosealicense.com)
* [Best-README-Template](https://github.com/othneildrew/Best-README-Template/tree/master)
* [Shields.io](https://shields.io/)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
