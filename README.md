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
</div>



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
      <a href="#screenshots">Screenshots</a>
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
- ❓ Open an [issue](https://github.com/C9Glax/tranga/issues)

and automatically import them with [Komga](https://komga.org/) and [Kavita](https://www.kavitareader.com/). Also Notifications will be sent to your devices using [Gotify](https://gotify.net/) and [LunaSea](https://www.lunasea.app/).
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


## Screenshots

| ![image](screenshots/overview.png) | ![image](screenshots/addtask.png) |
|-----------------------------------:|:----------------------------------|

| ![image](screenshots/settings.png) | ![image](screenshots/publication-description.png) | ![image](screenshots/progress.png) |
|-----------------------------------:|:-------------------------------------------------:|:-----------------------------------|

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
## Getting Started

There is two release types:

- CLI
- Docker

### CLI

Head over to [releases](https://git.bernloehr.eu/glax/Tranga/releases) and download. The CLI will guide you through setup.

### Docker

Download [docker-compose.yaml](https://git.bernloehr.eu/glax/Tranga/src/branch/master/docker-compose.yaml) and configure to your needs.

Wherever you are mounting `/usr/share/Tranga-API` you also need to mount that same path + `/imageCache` in the webserver container.

### Docker-Website usage

There is two ways to download Mangas:
- Downloading everything and monitor for new Chapters
- Selecting specific Volumes/Chapters

On the website you add new tasks, by selecting the blue '+' field. Next select the connector/site you want to use, and enter a search term.
After clicking 'Search' (or pressing Enter), the results will be presented below - this might, depending on the result-size, take a while because we are already preloading the cover-images.
Next select the publication (by selecting the cover) and a new popup will open with two options:
- "Monitor" - Download all chapters and monitor for new ones
- "Download Chapter" - Download specific chapters only

When selecting `Monitor` you will be presented with a new window and the selection of the interval you want to check for new chapters (Default: Every 3 hours).
When selecting `Download Chapter` a list will open with all available chapters - that have not yet been downloaded - from which you can then select a range (see below).

The syntax for selecting chapters is as follows:
- To download a single Chapter enter either the index number (the number at the very start of the line) or its absolute number like so: `c(h)(apter)[number]`, spaces are allowed.
- To download a range of chapters enter either a range of index numbers (`3-6`) or chapters (`ch 12-23`).
- For volumes the syntax is as follows: `v(ol)[number](-[number])`, again spaces allowed.

Examples: `2-12`, `c1`, `ch 2`, `chapter 3`, `v 2`, `vol3-4`, `v2c4` (note: you can only specify a single chapter with this last syntax).

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
