# syntax=docker/dockerfile:1
ARG DOTNET=10.0
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET AS base

# Temporarily switch to root for Chromium install
USER root

# Add ppa:xtradeb/apps for non-snap Chromium install
RUN apt-get update \
    && apt-get install -y software-properties-common \
    && add-apt-repository ppa:xtradeb/apps

RUN apt-get update \
  && apt-get install -y libx11-6 libx11-xcb1 libatk1.0-0t64 libgtk-3-0t64 libcups2t64 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2t64 libxshmfence1 libnss3 ungoogled-chromium \
  && apt-get autopurge -y \
  && apt-get autoclean -y \
  && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:$DOTNET AS build-env
WORKDIR /src
COPY Tranga.sln /src
COPY API/API.csproj /src/API/API.csproj
RUN dotnet restore /src/API/API.csproj

COPY . /src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish /src/API/API.csproj -c Release --property:OutputPath=/publish -maxcpucount:1 --no-cache

FROM base AS runtime
WORKDIR /publish

# Expose port
EXPOSE 6531

# User setup
ARG UNAME=tranga
ARG UID=1000
ARG GID=1000
RUN groupadd -g $GID -o $UNAME \
  && useradd -m -u $UID -g $GID -o -s /bin/bash $UNAME \
  && mkdir /usr/share/tranga-api \
  && mkdir /Manga \
  && chown 1000:1000 /usr/share/tranga-api \
  && chown 1000:1000 /Manga \
  # Ensure Chromium is executable
  && chmod +x /usr/bin/ungoogled-chromium

USER $UNAME

# Env vars for PuppeteerSharp (Chromium path + no-sandbox args)
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/ungoogled-chromium
ENV CHROME_BIN=/usr/bin/ungoogled-chromium
ENV PUPPETEER_ARGS="--no-sandbox --disable-setuid-sandbox --disable-dev-shm-usage --disable-gpu --no-zygote --single-process"


WORKDIR /publish
COPY --chown=1000:1000 --from=build-env /publish .

# Root for entrypoint if needed
USER 0
ENTRYPOINT ["dotnet", "/publish/API.dll"]
CMD [""]