# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
WORKDIR /src
COPY CLI /src/CLI
COPY Tranga /src/Tranga
COPY Logging /src/Logging
COPY Tranga.sln /src
RUN dotnet restore /src/Tranga/Tranga.csproj
RUN dotnet publish -c Release -o /publish

FROM glax/tranga-base:latest as runtime
EXPOSE 6531
ARG UNAME=tranga UID=1000 GID=1000
RUN groupadd -g $GID -o $UNAME && useradd -m -u $UID -g $GID -o -s /bin/bash $UNAME
RUN mkdir -p /usr/share/tranga-api /Manga && chown $UID:$GID /usr/share/tranga-api /Manga
WORKDIR /publish
COPY --from=build-env /publish .
USER 0
RUN chown $UID:$GID /publish
ENTRYPOINT ["dotnet", "/publish/Tranga.dll", "-f", "-c", "-l", "/usr/share/tranga-api/logs"]
