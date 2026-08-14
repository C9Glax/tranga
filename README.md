<span id="readme-top"></span>
<div align="center">

  <h1 align="center">Tranga</h1>
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

## Features

- [x] Monitor providers for new chapters and download new ones
- [x] Enrich Manga and chapters with Metadata from third-party websites
- [x] Flaresolverr support
- [x] Notifications
  - Using [Naprise](https://github.com/Genteure/naprise)
    - Gotify
    - Telegram
    - Discord
    - Ntfy.sh
    - ...
- [x] Library support using [Komga](https://komga.org/)
  - Sync Metadata
  - Scan chapters
  - Sync Cover images
- [x] Download sources via a bundled [Suwayomi](https://github.com/Suwayomi/Suwayomi-Server) sidecar, giving access to
  the ~2000 sources of the [keiyoushi](https://github.com/keiyoushi/extensions) extension repository

#### TODO LIST

- [ ] Unified webpage scraper ([Common/Helpers/RequestClient.cs](Common/Helpers/RequestClient.cs))
- [ ] Accounts (Authorization) (`Services.Users`)
- [ ] Existing chapter mapping (`Services.Manga`)

## Getting started

- [docker-compose.yaml](Tranga.AppHost/aspire-output/docker-compose.yaml)
You probably do not want to modify this. Use the [`.env`](Tranga.AppHost/aspire-output/.env) file for configuration.
- [.env](Tranga.AppHost/aspire-output/.env)
  - [Environment variable documentation](EnvVars.md)

`Mangas` is bind-mounted from the host path set in `MangaDirectory`, so it keeps whatever ownership that host directory already has.
The services write to it as a non-root container user (UID/GID `1654`), so if you hit `UnauthorizedAccessException`/`Permission denied` errors on download,
chown the host directory before starting the stack:

```bash
sudo chown -R 1654:1654 /path/to/your/Manga
```

### Download sources (Suwayomi + keiyoushi)

Apart from MangaDex, which is built in, Tranga gets its download sources from a bundled
[Suwayomi](https://github.com/Suwayomi/Suwayomi-Server) sidecar. Suwayomi executes the Android extension APKs of the
[keiyoushi](https://github.com/keiyoushi/extensions) repository on the JVM — around 1400 extensions covering 2000
sources — and Tranga drives it over its API. The container starts with the rest of the stack; there is nothing to
switch on.

Install the sources you want under **Settings → Sources**. Each one then behaves like any other download extension:
searchable, attachable as a download link, and downloaded by the normal task pipeline.

The sidecar is reachable only by Tranga's own services on the internal network — it is not exposed through the gateway,
and its own WebUI is switched off. That is deliberate: the gateway does not authenticate (each service checks
credentials itself), so anything routed straight to the sidecar would bypass `UseAuth` entirely.

If FlareSolverr is configured for Tranga, the sidecar is pointed at the same instance.

> The `Suwayomi` volume is persistent state, not a cache. It holds the installed extension JARs as well as the rows
> that map manga URLs back to the sidecar's internal ids. Deleting it means reinstalling your extensions.

### Adding a Komga library

When adding a Komga library, the "Library Root Path" must be the path *inside the Komga container*
where Tranga's `Mangas` volume is mounted — not the path on the host, and not the path inside Tranga's own containers.
Mount that same volume into your Komga container and point the field at wherever you mounted it (defaults to `/tranga` if left blank).

### Resetting the admin password

If `UseAuth` is enabled and you've forgotten the admin password, or the account is locked out after too many failed logins,
run the `reset-password` tool shipped inside the auth container:

```bash
docker exec -it <auth-container> /app/reset-password
```

It prompts for a new password (input hidden, entered twice to confirm) and clears any active lockout.
Leaving the prompt empty removes the password entirely, putting the deployment back into first-run setup —
the login page will show the setup screen again on next load.

## Screenshots

<img src="Screenshots/startpage.png" width="512" alt="start page"/>
<img src="Screenshots/search.png" width="512" alt="search dialog"/>
<img src="Screenshots/tasks.png" width="512" alt="tasks page"/>
<img src="Screenshots/manga.png" width="512" alt="manga page"/>
<img src="Screenshots/manga-light.png" width="512" alt="light mode manga page"/>

## Built With

**💙 [Blåhaj](https://www.ikea.com/us/en/p/blahaj-soft-toy-shark-90373590/) 🦈**
- [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet)
  - [EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL](https://www.postgresql.org/about/licence/)
  - [Ngpsql](https://github.com/npgsql/npgsql/blob/main/LICENSE)
- [Sixlabors.ImageSharp](https://docs-v2.sixlabors.com/articles/imagesharp/index.html#license)
- [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr)
  - [FlareSolverrSharp](https://github.com/FlareSolverr/FlareSolverrSharp)
- [Suwayomi-Server](https://github.com/Suwayomi/Suwayomi-Server)
  - [keiyoushi extensions](https://github.com/keiyoushi/extensions)
- [Naprise](https://github.com/Genteure/naprise)
- [BuildInformation](https://github.com/linkdotnet/BuildInformation)
- [GitInfo](https://github.com/devlooped/GitInfo)
- [xUnit](https://xunit.net/index.html?tabs=cs)
- [XmlSchemaClassGenerator](https://github.com/mganss/XmlSchemaClassGenerator)
- [NSwag](https://github.com/RicoSuter/NSwag)
- [Html Agility Pack (HAP)](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [Nuxt](https://nuxt.com/)
  - [Nuxt UI](https://ui.nuxt.com/)
- [TailwindCSS](https://tailwindcss.com/)
- [Lucide](https://lucide.dev/)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Star History

<a href="https://www.star-history.com/?type=date&repos=c9glax%2Ftranga">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=c9glax/tranga&type=date&theme=dark&legend=top-left&sealed_token=VbMkUY1g0Rt0cT5bw0BVpVuK-HuG5UfMb-tt-AWbnWEZ71V1dCCM4hO8SBJuw7jfkpRXM4URm4Djm-jPoJ29CJjW-y8xDQZu1FtqIdgxbnbcEmIfAsFpGwUBbGwayoz7S7EwYaIjk7OoMz5z_Gd_ITaxUuESPxvjlAuZ5chRI5wtJB9vi7Lo6a7clWeP" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=c9glax/tranga&type=date&legend=top-left&sealed_token=VbMkUY1g0Rt0cT5bw0BVpVuK-HuG5UfMb-tt-AWbnWEZ71V1dCCM4hO8SBJuw7jfkpRXM4URm4Djm-jPoJ29CJjW-y8xDQZu1FtqIdgxbnbcEmIfAsFpGwUBbGwayoz7S7EwYaIjk7OoMz5z_Gd_ITaxUuESPxvjlAuZ5chRI5wtJB9vi7Lo6a7clWeP" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=c9glax/tranga&type=date&legend=top-left&sealed_token=VbMkUY1g0Rt0cT5bw0BVpVuK-HuG5UfMb-tt-AWbnWEZ71V1dCCM4hO8SBJuw7jfkpRXM4URm4Djm-jPoJ29CJjW-y8xDQZu1FtqIdgxbnbcEmIfAsFpGwUBbGwayoz7S7EwYaIjk7OoMz5z_Gd_ITaxUuESPxvjlAuZ5chRI5wtJB9vi7Lo6a7clWeP" />
 </picture>
</a>

### Prerequisits

[.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

<!-- CONTRIBUTING -->
## Contributing

If you want to contribute, please feel free to fork and create a Pull-Request!

Please read [CONTRIBUTING](CONTRIBUTING.md)

## License

Distributed under the GNU GPLv3  License. See [LICENSE](https://github.com/C9Glax/tranga/blob/main/LICENSE) for more information.

## Acknowledgments

* [Choose an Open Source License](https://choosealicense.com)
* [Best-README-Template](https://github.com/othneildrew/Best-README-Template/tree/master)
* [Shields.io](https://shields.io/)
