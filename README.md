<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->

<!-- PROJECT LOGO -->
<br />
<div align="center">

<h3 align="center">Tranga</h3>

  <p align="center">
    Automatic Manga and Metadata downloader 
  </p>
  <p align="center">
    This is the API for <a href="https://github.com/C9Glax/tranga-website">Tranga-Website</a>  
  </p>
</div>

# Important for existing users:
Tranga just had a complete rewrite. Old settings, tasks, etc. will not work.
For the time being the docker-tag `latest` will be the old, discontinued branch. `cuttingedge` is the active branch and
will soon be moved to the `latest` branch. There is no migration-tool. Make a backup of old files.

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Usage</a></li>
        <li><a href="#prerequisites">Prerequisites</a></li>
      </ul>
    </li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

Tranga can download Chapters and Metadata from "Scanlation" sites such as 

- [MangaDex.org](https://mangadex.org/)
- [Manganato.com](https://manganato.com/)
- [Mangasee](https://mangasee123.com/)
- [MangaKatana](https://mangakatana.com)
- ❓ Open an [issue](https://github.com/C9Glax/tranga/issues)

and trigger an scan with [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/).  
Notifications will can sent to your devices using [Gotify](https://gotify.net/) and [LunaSea](https://www.lunasea.app/).

### What this does and doesn't do

Tranga (this git-repo) will open a port (standard 6531) and listen for requests to add Jobs to Monitor and/or download specific Manga.
The configuration is all done through HTTP-Requests.
The frontend in this repo is **CLI**-based.  
_**For a web-frontend use [tranga-website](https://github.com/C9Glax/tranga-website).**_

This project downloads the images for a Manga from the specified Scanlation-Website and packages them with some metadata - from that same website - in a .cbz-archive (per chapter).  
It does this on an interval, and checks for any Chapters (.cbz-Archive) not already existing in your specified Download-Location. (If you rename or move files, it will download those again)  
Tranga can (if configured) trigger a scan in Komga or Kavita, however the directory in which the Manga reside has to be available to both Tranga and Komga/Kavita.

The project doesn't manage metadata, doesn't curate, change or enhance any information that isn't available on the selected Scanlation-Site.  
It will blindly use whatever is scrapes (yes this is a glorified Web-scraper).


### Inspiration:

Because [Kaizoku](https://github.com/oae/kaizoku) was relying on [mangal](https://github.com/metafates/mangal) and mangal
hasn't received bugfixes for it's issues with Titles not showing up, or throwing errors because of illegal characters,
there were no alternatives for automatic downloads. However [Kaizoku](https://github.com/oae/kaizoku) certainly had a great Web-UI.

That is why I wanted to create my own project, in a language I understand, and that I am able to maintain myself.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

- .NET-Core
- Newtonsoft.JSON
- [PuppeteerSharp](https://www.puppeteersharp.com/)
- [Html Agility Pack (HAP)](https://html-agility-pack.net/)
- 💙 Blåhaj 🦈

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
## Getting Started

There is two release types:

- CLI
- Docker

### CLI

Head over to [releases](https://git.bernloehr.eu/glax/Tranga/releases) and download.


~~The CLI will guide you through setup.~~ Not in the current version.  
Right now it is barebones with options to view logs and make HTTP-Requests

### Docker

Download [docker-compose.yaml](https://git.bernloehr.eu/glax/Tranga/src/branch/master/docker-compose.yaml) and configure to your needs.  
Mount `/Manga` to wherever you want your chapters (`.cbz`-Archives) downloaded (for exampled where Komga/Kavita can access them).  
The `docker-compose` also includes [tranga-website](https://github.com/C9Glax/tranga-website) as frontend. For its configuration refer to the repo README.

### Prerequisites

#### To Build
[.NET-Core 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
#### To Run
[.NET-Core 7.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) scroll down a bit, should be on the right the second item.

<!-- ROADMAP -->
## Roadmap

- [ ] Docker ARM support
- [ ] ❓

See the [open issues](https://github.com/C9Glax/tranga/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

The following is copy & pasted:

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the GNU GPLv3  License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Choose an Open Source License](https://choosealicense.com)
* [Font Awesome](https://fontawesome.com)
* [Best-README-Template](https://github.com/othneildrew/Best-README-Template/tree/master)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
