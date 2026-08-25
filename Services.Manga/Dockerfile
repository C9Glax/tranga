# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
RUN mkdir -p /app/Covers && chown $APP_UID /app/Covers
USER $APP_UID
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services.Manga/Services.Manga.csproj", "Services.Manga/"]
COPY ["Common.Services/Common.Services.csproj", "Common.Services/"]
COPY ["Tranga.ServiceDefaults/Tranga.ServiceDefaults.csproj", "Tranga.ServiceDefaults/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Common.Database/Common.Database.csproj", "Common.Database/"]
COPY ["Extensions/Extensions.csproj", "Extensions/"]
COPY ["GeneratedExtensionClients/GeneratedExtensionClients.csproj", "GeneratedExtensionClients/"]
RUN dotnet restore "Services.Manga/Services.Manga.csproj"
COPY . .
WORKDIR "/src/Services.Manga"
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet build "./Services.Manga.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet publish "./Services.Manga.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Services.Manga.dll"]
