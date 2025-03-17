<span id="readme-top"></span>
<div align="center">

  <h1 align="center">Tranga v2</h1>
  <p align="center">
    Automatic Manga and Metadata downloader 
  </p>
  
  ![GitHub License](https://img.shields.io/github/license/C9glax/tranga)
  
  <table>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/master?label=master"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-master.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/cuttingedge?label=cuttingedge"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-cuttingedge.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
    <tr>
      <th><img alt="GitHub branch check runs" src="https://img.shields.io/github/check-runs/c9glax/tranga/postgres-Server-V2?label=postgres-Server-V2"></th>
      <td><img alt="Last Run" src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Fc9glax%2Ftranga%2Factions%2Fworkflows%2Fdocker-image-serverv2.yml%2Fruns%3Fper_page%3D1&query=workflow_runs%5B0%5D.created_at&label=Last%20Run"></td>
    </tr>
  </table>

</div>

<!-- ABOUT THE PROJECT -->
## About The Project

Tranga can download Chapters and Metadata from "Scanlation" sites such as 

- [MangaDex.org](https://mangadex.org/) (Multilingual)
- [Manganato.gg](https://manganato.com/) (en) (or natomanga.com, mangakakalot, nelomanga, ...)
- [MangaKatana.com](https://mangakatana.com) (en)
- [Mangaworld.bz](https://www.mangaworld.bz/) (it)
- [Bato.to](https://bato.to/v3x) (en)
- [ManhuaPlus](https://manhuaplus.org/) (en)
- [MangaHere](https://www.mangahere.cc/) (en)
- [Weebcentral](https://weebcentral.com) (en)
- [Webtoons](https://www.webtoons.com/en/) (en)
- ❓ Open an [issue](https://github.com/C9Glax/tranga/issues/new?assignees=&labels=New+Connector&projects=&template=new_connector.yml&title=%5BNew+Connector%5D%3A+)

and trigger a library-scan with [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/).  
Notifications can be sent to your devices using [Gotify](https://gotify.net/), [LunaSea](https://www.lunasea.app/) or [Ntfy](https://ntfy.sh/
), or any other service that can use REST Webhooks.

## What this program does and does *not* do

Tranga (the program in this repository) is a REST-API and worker in one. Meaning it will open a network-port
to listen for requests, and then work through these. Requests include searches for Manga, starting "Jobs" such
as downloading available chapters, creating a monitoring job (that will periodically do the aforementioned),
update metadata, and more.

This repository *does not* include a frontend. A frontend can take many forms, such as a website:

[tranga-website](https://github.com/C9Glax/tranga-website)

When downloading a chapter (meaning the images that make-up the manga) from a Scanlation-Website, Tranga will
additionally try and scrape Metadata from the same website ~~or enhance it from third-party sources~~
(tbd https://github.com/C9Glax/tranga/issues/280).
Downloaded images can be jpeg-compressed and/or made black and white to save on diskspace
(measured at least a 50% reduction in size, without a significant loss of quality).

Tranga will then package the contents of each chapter in a `.cbz`-archive and place it in a common folder per Manga.
If specified, Tranga will then notify library-Managers such as [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/) to trigger a scan for new
chapters. Tranga can also send notifications to your devices via third-party services such as [Gotify](https://gotify.net/), [LunaSea](https://www.lunasea.app/) or [Ntfy](https://ntfy.sh/
).

## Screenshots

This repository has no frontend, however checkout [tranga-website](https://github.com/C9Glax/tranga-website) for a default!

## Inspiration:

Because [Kaizoku](https://github.com/oae/kaizoku) was relying on [mangal](https://github.com/metafates/mangal) and mangal
hasn't received bugfixes for its issues with Titles not showing up, or throwing errors because of illegal characters,
there were no alternatives for automatic downloads. However, [Kaizoku](https://github.com/oae/kaizoku) certainly had a great Web-UI.

That is why I wanted to create my own project, in a language I understand, and that I am able to maintain myself.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Endpoint Documentation

Endpoints are documented in Swagger. Just spin up an instance, and go to `http://<url>/swagger`.

## Built With

- .NET
  - ASP.NET
  - Entity Framework
- [PostgreSQL](https://www.postgresql.org/about/licence/)
- [Swagger](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE)
- [Ngpsql](https://github.com/npgsql/npgsql/blob/main/LICENSE)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
- [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp/blob/master/LICENSE)
- [Html Agility Pack (HAP)](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [Soenneker.Utils.String.NeedlemanWunsch](https://github.com/soenneker/soenneker.utils.string.needlemanwunsch/blob/main/LICENSE)
- [Sixlabors.ImageSharp](https://docs-v2.sixlabors.com/articles/imagesharp/index.html#license)
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

An example `docker-compose.yaml` is provided. Mount `/Manga` to wherever you want your chapters (`.cbz`-Archives)
downloaded (where Komga/Kavita can access them for example).  
The file also includes [tranga-website](https://github.com/C9Glax/tranga-website) as frontend. For its configuration refer to the
[Tranga-Website Repository](https://github.com/C9Glax/tranga-website) README.

For compatibility do not execute the compose as root (which you should not do anyways...) but as user that can
access the folder. Permission conflicts with Komga and Kavita should thus be limited.

### Bare-Metal

While not supported/currently built, Tranga will also run Bare-Metal without issue.

Configuration-Files will be stored per OS:
- Linux `/usr/share/tranga-api`
- Windows `%appdata%/tranga-api`

Downloads (default) are stored in - but this can be configured in `settings.json`:
- Linux `/Manga`
- Windows `%currentDirectory%/Downloads`

#### Prerequisits

[.NET-Core 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

<!-- CONTRIBUTING -->
## Contributing

If you want to contribute, please feel free to fork and create a Pull-Request!

General rules:
- Strongly-type your variables. This improves readability.
```csharp
var xyz = Object.GetSomething(); //Do not do this. What type is xyz?
Manga[] zyx = Object.GetAnotherThing(); //I can now easily see that zyx is an Array.
```

**A broad overview of where is what:**<br />

- `Program.cs` Configuration for ASP.NET, Swagger (also in `NamedSwaggerGenOptions.cs`, Npgsql
- `Tranga.cs` Job(worker)-Logic
- `Schema/` Entity-Framework
  - `Schema/Jobs/` + Logic for Jobs
  - `Schema/**/` + Logic for **
  - `Schema/PgsqlContext.cs` EF configuration
- `MangaDownloadClients/` Networking-Clients for Scraping
- `Controllers/` ASP.NET Controllers (Endpoints)
- `APIEndpointRecords/` Records for API-Requests with specific Request-Types (Body)

If you want to add a new Scanlationsite-Connector: <br />
1. Copy one of the existing connectors, or start from scratch and inherit from `API.Schema.MangaConnectors.MangaConnector`.
2. Add the new Connector as Object-Instance in `Program.cs` to the MangaConnector-Array `connectors`.
3. In `Schema/PgsqlContext.cs` add the Discriminator for the Connector (the value is the name of the connector, as defined
in the constructor).

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
