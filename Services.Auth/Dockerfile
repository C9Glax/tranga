# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services.Auth/Services.Auth.csproj", "Services.Auth/"]
COPY ["Services.Auth.ResetPasswordTool/Services.Auth.ResetPasswordTool.csproj", "Services.Auth.ResetPasswordTool/"]
COPY ["Common.Services/Common.Services.csproj", "Common.Services/"]
COPY ["Tranga.ServiceDefaults/Tranga.ServiceDefaults.csproj", "Tranga.ServiceDefaults/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Common.Database/Common.Database.csproj", "Common.Database/"]
COPY ["Extensions/Extensions.csproj", "Extensions/"]
COPY ["GeneratedExtensionClients/GeneratedExtensionClients.csproj", "GeneratedExtensionClients/"]
RUN dotnet restore "Services.Auth/Services.Auth.csproj"
RUN dotnet restore "Services.Auth.ResetPasswordTool/Services.Auth.ResetPasswordTool.csproj"
COPY . .
WORKDIR "/src/Services.Auth"
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet build "./Services.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic
WORKDIR "/src/Services.Auth.ResetPasswordTool"
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet build "./Services.Auth.ResetPasswordTool.csproj" -c $BUILD_CONFIGURATION -o /app/build-tools /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet publish "/src/Services.Auth/Services.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic
RUN --mount=type=secret,id=sixlabors_lic \
    dotnet publish "/src/Services.Auth.ResetPasswordTool/Services.Auth.ResetPasswordTool.csproj" -c $BUILD_CONFIGURATION -o /app/publish/tools /p:UseAppHost=false /p:SixLaborsLicenseFile=/run/secrets/sixlabors_lic

FROM base AS final
USER root
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["Services.Auth/reset-password.sh", "/app/reset-password"]
RUN chmod +x /app/reset-password
USER $APP_UID
ENTRYPOINT ["dotnet", "Services.Auth.dll"]
