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
EXPOSE 6531
ARG UNAME=tranga
ARG UID=1000
ARG GID=1000
RUN groupadd -g $GID -o $UNAME \
  && useradd -m -u $UID -g $GID -o -s /bin/bash $UNAME \
  && mkdir /usr/share/tranga-api \
  && mkdir /Manga \
  && chown 1000:1000 /usr/share/tranga-api \
  && chown 1000:1000 /Manga 
USER $UNAME

WORKDIR /publish
COPY --chown=1000:1000 --from=build-env /publish .
USER 0
ENTRYPOINT ["dotnet", "/publish/API.dll"]
CMD [""]