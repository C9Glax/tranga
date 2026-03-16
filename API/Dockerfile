FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["API/API.csproj", "API/"]
COPY ["Common.Datatypes/Common.Datatypes.csproj", "Common.Datatypes/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["DownloadExtensions/DownloadExtensions.csproj", "DownloadExtensions/"]
COPY ["Common.Helpers/Common.Helpers.csproj", "Common.Helpers/"]
COPY ["NSwagClients/NSwagClients.csproj", "NSwagClients/"]
COPY ["MetadataExtensions/MetadataExtensions.csproj", "MetadataExtensions/"]
RUN dotnet restore "API/API.csproj"
COPY . .
WORKDIR "/src/API"
RUN dotnet build "./API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
