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

## Getting started

- [docker-compose.yaml](docker-compose.yaml) You probably do not want to modify this. Use the `.env` file for configuration.
- [.env](.env)
  - [Environment Variables](EnvVars.md)

### Volumes/Bindings

- `/app/Mangas` Download directory
- `/app/Covers` Cover directory

## Built With

**💙 [Blåhaj](https://www.ikea.com/us/en/p/blahaj-soft-toy-shark-90373590/) 🦈**
- [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet)
  - [EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL](https://www.postgresql.org/about/licence/)
  - [Ngpsql](https://github.com/npgsql/npgsql/blob/main/LICENSE)
- [Sixlabors.ImageSharp](https://docs-v2.sixlabors.com/articles/imagesharp/index.html#license)
- [Html Agility Pack (HAP)](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [BuildInformation](https://github.com/linkdotnet/BuildInformation)
- [GitInfo](https://github.com/devlooped/GitInfo)
- [xUnit](https://xunit.net/index.html?tabs=cs)
- [XmlSchemaClassGenerator](https://github.com/mganss/XmlSchemaClassGenerator)
- [NSwag](https://github.com/RicoSuter/NSwag)
- [Nuxt](https://nuxt.com/)
  - [Nuxt UI](https://ui.nuxt.com/)
- [TailwindCSS](https://tailwindcss.com/)
- [Lucide](https://lucide.dev/)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Star History

<a href="https://star-history.com/#c9glax/tranga&Date">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date&theme=dark" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=c9glax/tranga&type=Date" />
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
