FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Application/Application.csproj", "Application/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Common.Datatypes/Common.Datatypes.csproj", "Common.Datatypes/"]
COPY ["DownloadExtensions/DownloadExtensions.csproj", "DownloadExtensions/"]
COPY ["Common.Helpers/Common.Helpers.csproj", "Common.Helpers/"]
COPY ["Settings/Settings.csproj", "Settings/"]
COPY ["NSwagClients/NSwagClients.csproj", "NSwagClients/"]
COPY ["MetadataExtensions/MetadataExtensions.csproj", "MetadataExtensions/"]
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "Application/Application.csproj"
COPY . .
WORKDIR "/src/Application"
RUN dotnet build "./Application.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Application.dll"]
