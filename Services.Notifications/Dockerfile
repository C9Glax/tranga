# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services.Notifications/Services.Notifications.csproj", "Services.Notifications/"]
COPY ["Common.Services/Common.Services.csproj", "Common.Services/"]
COPY ["Tranga.ServiceDefaults/Tranga.ServiceDefaults.csproj", "Tranga.ServiceDefaults/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Extensions/Extensions.csproj", "Extensions/"]
COPY ["GeneratedExtensionClients/GeneratedExtensionClients.csproj", "GeneratedExtensionClients/"]
COPY ["Common.Database/Common.Database.csproj", "Common.Database/"]
RUN dotnet restore "Services.Notifications/Services.Notifications.csproj"
COPY . .
WORKDIR "/src/Services.Notifications"
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet build "./Services.Notifications.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet publish "./Services.Notifications.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Services.Notifications.dll"]
