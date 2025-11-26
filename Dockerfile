# syntax=docker/dockerfile:1
ARG DOTNET=9.0
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET AS base
WORKDIR /publish

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:$DOTNET AS build-env
WORKDIR /src
COPY Tranga.sln /src
COPY API/API.csproj /src/API/API.csproj
RUN dotnet restore /src/Tranga.sln
COPY . /src/
RUN dotnet publish -c Release --property:OutputPath=/publish -maxcpucount:1

FROM base AS runtime

# Temporarily switch to root for Chromium install
USER root

RUN apt-get update \
  && apt-get install -y libx11-6 libx11-xcb1 libatk1.0-0 libgtk-3-0 libcups2 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2 libxshmfence1 libnss3 chromium \
  && apt-get autopurge -y \
  && apt-get autoclean -y \
  && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

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
  && chmod +x /usr/bin/chromium

USER $UNAME

# Env vars for PuppeteerSharp (Chromium path + no-sandbox args)
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV CHROME_BIN=/usr/bin/chromium
ENV PUPPETEER_ARGS="--no-sandbox --disable-setuid-sandbox --disable-dev-shm-usage --disable-gpu --no-zygote --single-process"


WORKDIR /publish
COPY --chown=1000:1000 --from=build-env /publish .

# Root for entrypoint if needed
USER 0
ENTRYPOINT ["dotnet", "/publish/API.dll"]
CMD [""]