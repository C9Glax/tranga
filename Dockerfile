# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
WORKDIR /src
COPY Tranga /src/Tranga
COPY Logging /src/Logging
COPY Tranga.sln /src
RUN dotnet restore /src/Tranga/Tranga.csproj
RUN dotnet publish -c Release -o /publish

FROM glax/tranga-base:latest as runtime
WORKDIR /publish
COPY --from=build-env /publish .
EXPOSE 6531
ENTRYPOINT ["dotnet", "/publish/Tranga.dll"]
