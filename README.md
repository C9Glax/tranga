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
- [AsuraComic](https://asurascanz.com) (en) thanks [@yacob841](https://github.com/yacob841)
- [WeebCentral](https://weebcentral.com/) (en) thanks [@TheyCallMeTravis](https://github.com/TheyCallMeTravis)
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

[tranga-website](https://github.com/C9Glax/tranga-website) (Original, if you use the `docker-compose.yaml` in this repo it will spin up an instance for this)

[tranga-yacobGUI](https://github.com/yacob841/tranga-yacobGUI) (Third Party)

When downloading a chapter (meaning the images that make-up the manga) from a Website, Tranga will
additionally try and fetch Metadata from the same website or enhance it from third-party sources.
Downloaded images can be jpeg-compressed and/or made black and white to save on diskspace
(measured at least a 50% reduction in size, without a significant loss of quality).

Tranga will then package the contents of each chapter in a `.cbz`-archive and place it in a common folder per Manga.
If specified, Tranga will then notify library-Managers such as [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/) to trigger a scan for new
chapters. Tranga can also send notifications to your devices via third-party services such as [Gotify](https://gotify.net/), [Ntfy](https://ntfy.sh/),
or any other REST Webhook.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

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

**💙 [Blåhaj](https://www.ikea.com/us/en/p/blahaj-soft-toy-shark-90373590/) 🦈**
- [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet)
  - [EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL](https://www.postgresql.org/about/licence/)
  - [Ngpsql](https://github.com/npgsql/npgsql/blob/main/LICENSE)
- [Swagger](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
- [Sixlabors.ImageSharp](https://docs-v2.sixlabors.com/articles/imagesharp/index.html#license)
- [Html Agility Pack (HAP)](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [Puppeteersharp](https://www.puppeteersharp.com/)
  - [Puppeteer](https://github.com/puppeteer/puppeteer) 
  - [Chromium](https://www.chromium.org/Home/)
- [Soenneker.Utils.String.NeedlemanWunsch](https://github.com/soenneker/soenneker.utils.string.needlemanwunsch/blob/main/LICENSE)
- [Jikan](https://jikan.moe/)
  - [Jikan.Net](https://github.com/Ervie/jikan.net)
- [BuildInformation](https://github.com/linkdotnet/BuildInformation)
- [GitInfo](https://github.com/devlooped/GitInfo)
- [Log4Net](https://logging.apache.org/log4net/index.html)
- [xUnit](https://xunit.net/index.html?tabs=cs)

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

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Docker

Built for AMD64.
Also available for ARM64 and ARMv7 if it feels like it (dependencies aren't always your friend).

An example `docker-compose.yaml` is provided. Mount `/Manga` to wherever you want your chapters (`.cbz`-Archives)
downloaded (where Komga/Kavita can access them for example).  
The file also includes [tranga-website](https://github.com/C9Glax/tranga-website) as frontend. For its configuration refer to the
[Tranga-Website Repository](https://github.com/C9Glax/tranga-website) README.

| Environment Variable              | default          | Description                                                                                                      |
|-----------------------------------|------------------|------------------------------------------------------------------------------------------------------------------|
| PORT                              | `6531`           | Port for the API Endpoints (Don't change this unless you know what you are doing)                                |
| POSTGRES_HOST                     | `tranga-pg:5432` | host-address of postgres database (Don't change!)                                                                |
| POSTGRES_DB                       | `postgres`       | name of database                                                                                                 |
| POSTGRES_USER                     | `postgres`       | username used for database authentication                                                                        |
| POSTGRES_PASSWORD                 | `postgres`       | password used for database authentication                                                                        |
| DOWNLOAD_LOCATION                 | `/Manga`         | Target-Directory for Downloads (Path inside the container! Don't change!)                                        |
| FLARESOLVERR_URL                  | <empty>          | URL of Flaresolverr-Instance                                                                                     |
| POSTGRES_COMMAND_TIMEOUT          | `60`             | [Timeout of Postgres-commands](https://www.npgsql.org/doc/connection-string-parameters.html?q=Command%20Timeout) |
| POSTGRES_CONNECTION_TIMEOUT       | `30`             | Timeout for postgres-databaes connection                                                                         |
| CHECK_CHAPTERS_BEFORE_START       | `true`           | Whether to update database downloaded chapters column on startup (takes a while)                                 |
| MATCH_EXACT_CHAPTER_NAME          | `true`           | Match the stored filename exactly with the filename on disk when checking if a chapter is downloaded             |
| CREATE_COMICINFO_XML              | `true`           | Whether to include ComicInfo.xml in .cbz-Archives                                                                |
| ALWAYS_INCLUDE_VOLUME_IN_FILENAME | `false`          | Override to always include a volume in filenames (default as `Vol. 0`)                                           |
| HTTP_REQUEST_TIMEOUT              | `10`             | Request timeout for Mangaconnectors                                                                              |
| REQUESTS_PER_MINUTE               | `90`             | Maximum requests per minute for Mangaconnectors (Don't change)                                                   |
| MINUTES_BETWEEN_NOTIFICATIONS     | `1`              | Interval at which Tranga checks if notifications need to be sent.                                                |
| HOURS_BETWEEN_NEW_CHAPTERS_CHECK  | `3`              | Interval at which Tranga checks if there are new chapters for a manga                                            |
| WORKER_TIMEOUT                    | `600`            | Seconds a worker can take before being forcefully cancelled                                                      |

### Bare-Metal

While not intended, if your machine can run .NET (and is AMD64, ARM64, ARMv7), it can run Tranga without docker.

Configuration-Files are stored at
- Linux `/usr/share/tranga-api`
- Windows `%appdata%/tranga-api`

Downloads (default) are stored in (see `DOWNLOAD_LOCATION`)
- Linux `/Manga`
- Windows `%currentDirectory%/Downloads`

### Prerequisits

[.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

<!-- CONTRIBUTING -->
## Contributing

If you want to contribute, please feel free to fork and create a Pull-Request!

Please read [CONTRIBUTING](CONTRIBUTING.md)

<!-- LICENSE -->
## License

Distributed under the GNU GPLv3  License. See [LICENSE](https://github.com/C9Glax/tranga/blob/main/LICENSE) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Choose an Open Source License](https://choosealicense.com)
* [Best-README-Template](https://github.com/othneildrew/Best-README-Template/tree/master)
* [Shields.io](https://shields.io/)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
