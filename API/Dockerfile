# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
WORKDIR /src
COPY . /src/
RUN ls -l /src
RUN dotnet restore /src/API/API.csproj
RUN dotnet publish -c Release -o /publish

FROM glax/tranga-base:latest as runtime
WORKDIR /publish
COPY --from=build-env /publish .
EXPOSE 6531
ENTRYPOINT ["dotnet", "/publish/API.dll"]
