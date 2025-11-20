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

# Temporarily switch to root for Chrome install
USER root

RUN apt-get update \
    && apt-get install -y wget gnupg2 apt-utils ca-certificates fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgbm1 libgcc1 libglib2.0-0 libgtk-3-0 libnspr4 libnss3 libpango-1.0-0 libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 libxss1 libxtst6 lsb-release xdg-utils \
    # Add more fonts for full rendering (from PuppeteerSharp guides)
    && apt-get install -y fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
    # Add Google's Chrome repo for stable (avoids direct deb download/pinning)
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo 'deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main' >> /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable --no-install-recommends --allow-downgrades \
    # Clean up
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

RUN chmod 755 /usr/bin/google-chrome-stable

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
  # Ensure Chrome is executable by non-root user
  && chmod +x /usr/bin/google-chrome-stable

USER $UNAME

# Env vars for PuppeteerSharp (stable path + no-sandbox args to avoid download/init fails)
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable
ENV CHROME_BIN=/usr/bin/google-chrome-stable
ENV PUPPETEER_ARGS="--no-sandbox --disable-setuid-sandbox --disable-dev-shm-usage --disable-gpu --no-zygote --single-process"

WORKDIR /publish
COPY --chown=1000:1000 --from=build-env /publish .

# Root for entrypoint if needed
USER 0
ENTRYPOINT ["dotnet", "/publish/API.dll"]
CMD [""]